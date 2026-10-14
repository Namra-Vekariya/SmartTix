using System.Globalization;
using SmartTix.Application.DTOs.Ai;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Application.Services;

// Deterministic seat-picker — the LLM only parses the request; every seat
// decision made here is pure, testable code (no AI in the hot path).
//
// Scoring: horizontal centrality (x-axis) + depth into the hall (y-axis).
// Default depth target is ~2/3 of the way in, which is where people
// actually choose to sit when they state no preference.
internal static class SeatFinder
{
    private const int MaxOptions = 3;
    private const int MaxSeatCount = 10;
    private const double DefaultDepthTarget = 0.66;

    private sealed record Candidate(
        SeatRecommendation Recommendation,
        double Score,
        HashSet<string> SeatCodes);

    public static List<SeatRecommendation> Find(List<Seat> seats, AiSeatQuery query)
    {
        var seatCount = Math.Clamp(query.SeatCount, 1, MaxSeatCount);
        var maxTotalBudget = query.MaxTotalBudget is > 0 ? query.MaxTotalBudget : null;
        var maxPerSeatBudget = query.MaxPerSeatBudget is > 0 ? query.MaxPerSeatBudget : null;

        var available = seats
            .Where(s => s.Status == SeatStatus.Available)
            .ToList();

        if (available.Count < seatCount)
            return new List<SeatRecommendation>();

        // Hall geometry comes from ALL rows (not just available ones) so a
        // sold-out front section does not shift what "centre" means
        var allRowLabels = seats
            .Select(s => s.RowLabel)
            .Distinct()
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();
        var rowCount = allRowLabels.Count;

        var hallWidth = seats.Max(s => s.SeatNumber);
        var hallCentre = (hallWidth + 1) / 2.0;
        var maxCentreDistance = Math.Max((hallWidth - 1) / 2.0, 1.0);

        var horizontalWeight = query.Position switch
        {
            "center" => 0.7,
            "front" or "back" => 0.4,
            _ => 0.6
        };
        var depthTarget = query.Position switch
        {
            "front" => 0.0,
            "back" => 1.0,
            _ => DefaultDepthTarget
        };

        var candidates = new List<Candidate>();

        foreach (var row in available.GroupBy(s => s.RowLabel))
        {
            if (query.RowLabel is not null &&
                !row.Key.Equals(query.RowLabel.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var firstSeat = row.First();
            if (query.CategoryName is not null &&
                !firstSeat.SeatCategory.Name.Contains(query.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var pricePerSeat = firstSeat.SeatCategory.Price;
            var totalPrice = pricePerSeat * seatCount;

            if (maxPerSeatBudget is not null && pricePerSeat > maxPerSeatBudget.Value)
                continue;
            if (maxTotalBudget is not null && totalPrice > maxTotalBudget.Value)
                continue;

            foreach (var group in BuildGroups(row.ToList(), seatCount, query.Together, hallCentre))
            {
                var groupCentre = group.Average(n => (double)n);
                var centreDistance = Math.Abs(groupCentre - hallCentre);
                var horizontalScore = 1.0 - (centreDistance / maxCentreDistance);

                var rowIndex = allRowLabels.IndexOf(row.Key);
                var depth = rowCount > 1 ? (double)rowIndex / (rowCount - 1) : DefaultDepthTarget;
                var depthScore = 1.0 - Math.Abs(depth - depthTarget);

                var score = (horizontalScore * horizontalWeight) + (depthScore * (1 - horizontalWeight));

                var codes = group
                    .Select(n => $"{row.Key}-{n}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                candidates.Add(new Candidate(
                    new SeatRecommendation
                    {
                        SeatCodes = group.Select(n => $"{row.Key}-{n}").ToList(),
                        CategoryName = firstSeat.SeatCategory.Name,
                        TotalPrice = totalPrice,
                        Reason = BuildReason(row.Key, group, firstSeat.SeatCategory.Name,
                            pricePerSeat, totalPrice, seatCount, horizontalScore, depth)
                    },
                    score,
                    codes));
            }
        }

        var ordered = query.PreferCheapest
            ? candidates.OrderBy(c => c.Recommendation.TotalPrice).ThenByDescending(c => c.Score)
            : candidates.OrderByDescending(c => c.Score).ThenBy(c => c.Recommendation.TotalPrice);

        return SelectDistinct(ordered);
    }

    // Each row yields either every single seat, every consecutive window of
    // N seats (Together), or one best-centred block of N seats (not Together)
    private static IEnumerable<List<int>> BuildGroups(
        List<Seat> rowSeats, int seatCount, bool together, double hallCentre)
    {
        var numbers = rowSeats.Select(s => s.SeatNumber).OrderBy(n => n).ToList();

        if (seatCount == 1)
            return numbers.Select(n => new List<int> { n });

        if (together)
        {
            var groups = new List<List<int>>();
            for (var start = 0; start <= numbers.Count - seatCount; start++)
            {
                var window = numbers.Skip(start).Take(seatCount).ToList();
                var consecutive = window.Zip(window.Skip(1), (a, b) => b - a).All(gap => gap == 1);
                if (consecutive)
                    groups.Add(window);
            }
            return groups;
        }

        if (numbers.Count < seatCount)
            return Enumerable.Empty<List<int>>();

        // Not together: take the N seats closest to the horizontal centre
        return new List<List<int>>
        {
            numbers
                .OrderBy(n => Math.Abs(n - hallCentre))
                .Take(seatCount)
                .OrderBy(n => n)
                .ToList()
        };
    }

    // Top options must not repeat seats with each other so the customer
    // gets 3 genuinely different choices; overlap is only allowed as a
    // fallback when the hall is too full for 3 disjoint groups
    private static List<SeatRecommendation> SelectDistinct(IOrderedEnumerable<Candidate> ordered)
    {
        var picked = new List<Candidate>();
        foreach (var candidate in ordered)
        {
            if (picked.Count >= MaxOptions)
                break;
            if (picked.All(p => !p.SeatCodes.Overlaps(candidate.SeatCodes)))
                picked.Add(candidate);
        }

        if (picked.Count >= MaxOptions)
            return picked.Select(p => p.Recommendation).ToList();

        foreach (var candidate in ordered)
        {
            if (picked.Count >= MaxOptions)
                break;
            if (picked.All(p => !p.SeatCodes.SetEquals(candidate.SeatCodes)))
                picked.Add(candidate);
        }

        return picked.Select(p => p.Recommendation).ToList();
    }

    private static string BuildReason(
        string rowLabel,
        List<int> numbers,
        string categoryName,
        decimal pricePerSeat,
        decimal totalPrice,
        int seatCount,
        double horizontalScore,
        double depth)
    {
        var seatsText = numbers.Count == 1
            ? $"{rowLabel}-{numbers[0]}"
            : $"{rowLabel}-{numbers.First()} to {rowLabel}-{numbers.Last()}";

        var layout = seatCount == 1 ? "single seat" : $"{seatCount} together";

        var centreText = horizontalScore switch
        {
            >= 0.9 => "dead centre",
            >= 0.65 => "near centre",
            >= 0.35 => "off to the side",
            _ => "side section"
        };

        var depthText = depth switch
        {
            < 0.33 => "front section",
            < 0.66 => "mid hall",
            _ => "back section"
        };

        var priceText = seatCount == 1
            ? $"{categoryName} {FormatMoney(pricePerSeat)} each"
            : $"{categoryName} {FormatMoney(pricePerSeat)} each ({FormatMoney(totalPrice)} total)";

        return $"{seatsText} — {layout}, {centreText}, {depthText}, {priceText}";
    }

    private static string FormatMoney(decimal value) =>
        "₹" + value.ToString("N0", CultureInfo.InvariantCulture);
}
