namespace SudokuSolver;

public class LogicalStepDesc
{
    public readonly string desc;
    public readonly List<(int, int)> highlightCells;
    public readonly List<int> sourceCandidates;
    public readonly List<int> elimCandidates;
    public readonly List<(int, int)> strongLinks;
    public readonly List<(int, int)> weakLinks;
    public readonly List<LogicalStepDesc> subSteps;
    public readonly bool isSingle;

    public LogicalStepDesc(string desc, IEnumerable<int> sourceCandidates, IEnumerable<int> elimCandidates, bool sourceIsAIC = false, bool isSingle = false, List<LogicalStepDesc> subSteps = null)
    {
        sourceCandidates ??= [];
        elimCandidates ??= [];

        this.desc = desc;
        this.sourceCandidates = sourceCandidates.ToList();
        this.elimCandidates = elimCandidates.ToList();
        this.isSingle = isSingle;
        this.subSteps = subSteps;

        if (sourceIsAIC)
        {
            strongLinks = [];
            weakLinks = [];
            bool strong = false;
            int prevCandidate = -1;
            foreach (int curCandidate in sourceCandidates)
            {
                if (prevCandidate != -1)
                {
                    var candidatePair = (prevCandidate, curCandidate);
                    if (strong)
                    {
                        strongLinks.Add(candidatePair);
                    }
                    else
                    {
                        weakLinks.Add(candidatePair);
                    }
                }
                prevCandidate = curCandidate;
                strong = !strong;
            }
        }
    }

    public LogicalStepDesc(string desc, IEnumerable<(int, int)> highlightCells)
    {
        this.desc = desc;
        this.highlightCells = highlightCells.ToList();
    }

    public LogicalStepDesc(string desc, (int, int) highlightCell)
    {
        this.desc = desc;
        highlightCells = [highlightCell];
    }

    public override string ToString()
    {
        if (subSteps == null || subSteps.Count == 0)
        {
            return desc;
        }

        StringBuilder sb = new();
        sb.AppendLine(desc);
        foreach (var step in subSteps)
        {
            sb.Append("    ").AppendLine(step.ToString());
        }
        return sb.ToString();
    }

    private LogicalStepDesc(
        string desc,
        List<(int, int)> highlightCells,
        List<int> sourceCandidates,
        List<int> elimCandidates,
        List<(int, int)> strongLinks,
        List<(int, int)> weakLinks,
        List<LogicalStepDesc> subSteps,
        bool isSingle)
    {
        this.desc = desc;
        this.highlightCells = highlightCells;
        this.sourceCandidates = sourceCandidates;
        this.elimCandidates = elimCandidates;
        this.strongLinks = strongLinks;
        this.weakLinks = weakLinks;
        this.subSteps = subSteps;
        this.isSingle = isSingle;
    }

    public LogicalStepDesc WithPrefix(string descPrefix) =>
        new(descPrefix + desc, highlightCells, sourceCandidates, elimCandidates, strongLinks, weakLinks, subSteps, isSingle);
}

public static class LogicalStepDescUtil
{
    public static StringBuilder Append(this StringBuilder sb, IEnumerable<LogicalStepDesc> logicalStepDescs)
    {
        foreach (var desc in logicalStepDescs)
        {
            sb.Append(desc);
        }
        return sb;
    }

    public static IEnumerable<T> ToEnumerable<T>(this T value)
    {
        yield return value;
    }
}
