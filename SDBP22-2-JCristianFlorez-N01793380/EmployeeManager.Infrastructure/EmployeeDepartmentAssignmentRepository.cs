using System;
using System.Collections.Generic;
using System.Text;

using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure;

public class EmployeeDepartmentAssignmentRepository : IEmployeeDepartmentAssignmentRepository
{
    private readonly AppDbContext _context;

    public EmployeeDepartmentAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDepartmentAssignment?> GetById(int id, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeDepartmentAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);
    }

    public async Task<EmployeeDepartmentAssignment> Create(
        EmployeeDepartmentAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        _context.EmployeeDepartmentAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<EmployeeDepartmentAssignment?> Update(
        int id,
        DateTime assignmentDate,
        AssignmentStatus status,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (existing is null) return null;

        existing.AssignmentDate = assignmentDate;
        existing.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteIfExist(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (existing is null) return false;

        _context.EmployeeDepartmentAssignments.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Employee?> GetEmployee(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
    }

    public async Task<bool> DepartmentExists(int departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId, cancellationToken);
    }

    public async Task<bool> HasOtherActiveAssignment(
        int employeeId,
        int excludingAssignmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeDepartmentAssignments
            .AsNoTracking()
            .AnyAsync(
                a => a.EmployeeId == employeeId
                     && a.Status == AssignmentStatus.Active
                     && a.AssignmentId != excludingAssignmentId,
                cancellationToken);
    }
}