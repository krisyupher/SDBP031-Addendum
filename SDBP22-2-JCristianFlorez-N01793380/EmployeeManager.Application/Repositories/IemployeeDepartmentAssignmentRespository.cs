using System;
using System.Collections.Generic;
using System.Text;
using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Repositories;

public interface IEmployeeDepartmentAssignmentRepository
{
    Task<EmployeeDepartmentAssignment?> GetById(int assignmentId, CancellationToken cancellationToken = default);
    Task<EmployeeDepartmentAssignment> Create(EmployeeDepartmentAssignment assignment, CancellationToken cancellationToken = default);
    Task<EmployeeDepartmentAssignment?> Update(int id, DateTime assignmentDate, AssignmentStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteIfExist(int Id, CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployee(int Id, CancellationToken cancellationToken = default);
    Task<bool> DepartmentExists(int departmentId, CancellationToken cancellationToken = default);
    Task<bool> HasOtherActiveAssignment(int employeeId, int excludeAssignmentId, CancellationToken cancellationToken = default);
}