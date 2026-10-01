using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;
using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Dtos;

public record AssignmentResponse(
    int AssignmentId,
    int EmployeeId,
    int DepartmentId,
    DateTime AssignmentDate,
    AssignmentStatus Status
    );


public class CreateAssignmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive integer.")]
    public int DepartmentId { get; set; }

    [Required]
    public DateTime AssignmentDate { get; set; }
}

public class UpdateAssignmentRequest
{
    [Required]
    public DateTime AssignmentDate { get; set; }

    [Required]
    public AssignmentStatus Status { get; set; }
}