namespace SudokuSolver.Constraints;

public abstract class AbstractIndexerConstraint : Constraint
{
    public readonly List<(int, int)> cells;
    protected readonly HashSet<(int, int)> cellsHash;

    // Return the target candidate of the input indexer candidate.
    protected abstract (int, int, int) TargetCell(Solver solver, int i, int j, int v);

    // Return the indexer candidate that would target the input candidate.
    protected abstract (int, int, int) InvTargetCell(Solver solver, int i, int j, int v);

    protected AbstractIndexerConstraint(Solver solver, string options) : base(solver, options)
    {
        if (WIDTH != HEIGHT || WIDTH != MAX_VALUE)
        {
            throw new ArgumentException($"Indexer constraints are unsupported in non-square grids.");
        }

        var cellGroups = ParseCells(options);
        if (cellGroups.Count != 1)
        {
            throw new ArgumentException($"Indexer constraint expects exactly 1 cell group, got {cellGroups.Count} from \"{options}\".");
        }

        cells = cellGroups[0];
        cellsHash = new(cells);
    }

    // Completely handled by weak links
    public override bool EnforceConstraint(Solver solver, int i, int j, int value) => true;

    public override LogicResult InitLinks(Solver solver, List<LogicalStepDesc> logicalStepDescription)
    {
        var board = solver.Board;
        for (int i = 0; i < HEIGHT; i++)
        {
            for (int j = 0; j < WIDTH; j++)
            {
                uint mask = board[i, j];
                if (IsValueSet(mask))
                {
                    continue;
                }

                int minVal = MinValue(mask);
                int maxVal = MaxValue(mask);
                for (int v = minVal; v <= maxVal; v++)
                {
                    if (!HasValue(mask, v))
                    {
                        continue;
                    }
                    int cand0 = CandidateIndex(i, j, v);

                    if (cellsHash.Contains((i, j)))
                    {
                        var (ti, tj, tv) = TargetCell(solver, i, j, v);
                        if (ti != i || tj != j)
                        {
                            for (int wv = 1; wv <= MAX_VALUE; wv++)
                            {
                                if (wv != tv)
                                {
                                    solver.AddWeakLink(cand0, CandidateIndex(ti, tj, wv));
                                }
                            }
                        }
                    }

                    var (ii, ij, iv) = InvTargetCell(solver, i, j, v);
                    if (ii != i || ij != j)
                    {
                        if (cellsHash.Contains((ii, ij)))
                        {
                            for (int wv = 1; wv <= MAX_VALUE; wv++)
                            {
                                if (wv != iv)
                                {
                                    solver.AddWeakLink(cand0, CandidateIndex(ii, ij, wv));
                                }
                            }
                        }
                    }
                }
            }
        }
        return LogicResult.None;
    }

    public override LogicResult StepLogic(Solver solver, StringBuilder logicalStepDescription, bool isBruteForcing)
    {
        var board = solver.Board;

        bool changed = false;

        // For each indexer cell:
        // 1. Clear candidates from the indexer cell which can no longer be placed
        // 2. Clear target values from cells which this cell can no longer point to
        foreach (var (ii, ij) in cells)
        {
            List<int> elims = null;
            uint imask = board[ii, ij];
            for (int iv = 1; iv <= MAX_VALUE; iv++)
            {
                var (ti, tj, tv) = TargetCell(solver, ii, ij, iv);
                if (HasValue(imask, iv))
                {
                    if (!HasValue(board[ti, tj], tv))
                    {
                        // The indexer cell can be this value, but the target cell can't be set to the proper target value (so clear it)
                        elims ??= [];
                        elims.Add(solver.CandidateIndex(ii, ij, iv));
                    }
                }
                else
                {
                    if (HasValue(board[ti, tj], tv))
                    {
                        // The indexer cell cannot be this value, but the target cell can still be set to the target value (so clear it)
                        elims ??= [];
                        elims.Add(solver.CandidateIndex(ti, tj, tv));
                    }
                }
            }

            if (elims != null)
            {
                logicalStepDescription?.Append($"Evaluated {CellName(ii, ij)} => {solver.DescribeElims(elims)}");
                if (!solver.ClearCandidates(elims))
                {
                    return LogicResult.Invalid;
                }
                if (logicalStepDescription != null)
                {
                    return LogicResult.Changed;
                }
                changed = true;
            }
        }

        return changed ? LogicResult.Changed : LogicResult.None;
    }
}

[Constraint(DisplayName = "Row Indexer", ConsoleName = "rowindexer")]
public class RowIndexerConstraint(Solver sudokuSolver, string options)
    : AbstractIndexerConstraint(sudokuSolver, options)
{
    protected override (int, int, int) TargetCell(Solver solver, int i, int j, int v) => (v - 1, j, i + 1);
    protected override (int, int, int) InvTargetCell(Solver solver, int i, int j, int v) => (v - 1, j, i + 1);
}

[Constraint(DisplayName = "Col Indexer", ConsoleName = "colindexer")]
public class ColIndexerConstraint(Solver sudokuSolver, string options)
    : AbstractIndexerConstraint(sudokuSolver, options)
{
    protected override (int, int, int) TargetCell(Solver solver, int i, int j, int v) => (i, v - 1, j + 1);
    protected override (int, int, int) InvTargetCell(Solver solver, int i, int j, int v) => (i, v - 1, j + 1);
}

[Constraint(DisplayName = "Box Indexer", ConsoleName = "boxindexer")]
public class BoxIndexerConstraint(Solver solver, string options) : AbstractIndexerConstraint(solver, options)
{
    private readonly Dictionary<(int, int), SudokuGroup> regionMap = new();
    private readonly Dictionary<(int, int), int> regionIndex = new();

    public override LogicResult InitCandidates(Solver solver)
    {
        for (int i = 0; i < HEIGHT; i++)
        {
            for (int j = 0; j < WIDTH; j++)
            {
                SudokuGroup region = solver.CellToGroupMap[(i, j)].FirstOrDefault(g => g.GroupType == GroupType.Region);
                if (region != null)
                {
                    regionMap[(i, j)] = region;
                    regionIndex[(i, j)] = region.Cells.IndexOf((i, j));
                }
            }
        }

        return LogicResult.None;
    }

    protected override (int, int, int) TargetCell(Solver solver, int i, int j, int v)
    {
        if (!regionMap.TryGetValue((i, j), out SudokuGroup region))
        {
            return (i, j, v);
        }

        var targetCell = region.Cells[v - 1];
        return (targetCell.Item1, targetCell.Item2, regionIndex[(i, j)] + 1);
    }
    protected override (int, int, int) InvTargetCell(Solver solver, int i, int j, int v) => TargetCell(solver, i, j, v);
}
