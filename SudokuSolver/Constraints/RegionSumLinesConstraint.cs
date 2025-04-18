﻿namespace SudokuSolver.Constraints;

[Constraint(DisplayName = "Region Sum Lines", ConsoleName = "rsl")]
public class RegionSumLinesConstraint : EqualSumsConstraint
{
    public override string SpecificName => $"Region Sum Line from {CellName(lineCells[0])} - {CellName(lineCells[^1])}";

    public readonly List<(int, int)> lineCells;

    public RegionSumLinesConstraint(Solver solver, string options) : base(solver, options)
    {
        var cellGroups = ParseCells(options);
        if (cellGroups.Count != 1)
        {
            throw new ArgumentException($"Region Sum Lines constraint expects 1 cell group, got {cellGroups.Count}.");
        }

        lineCells = cellGroups[0];
    }

    protected override List<List<(int, int)>> GetCellGroups(Solver solver)
    {
        SudokuGroup lastRegion = null;
        List<List<(int, int)>> cellGroups = [];
        foreach (var cell in lineCells)
        {
            SudokuGroup curRegion = solver.Groups
                .FirstOrDefault(group => group.GroupType == GroupType.Region && group.Cells.Contains(cell));
            if (curRegion == null)
            {
                continue;
            }

            if (lastRegion != curRegion)
            {
                cellGroups.Add([]);
            }
            cellGroups[^1].Add(cell);
            lastRegion = curRegion;
        }

        return cellGroups;
    }
}
