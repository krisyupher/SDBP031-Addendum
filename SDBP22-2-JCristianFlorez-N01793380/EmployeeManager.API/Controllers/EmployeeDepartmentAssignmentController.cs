using EmployeeManager.Application.Dtos;
using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.API.Controllers
{
    [Route("api/assignment")]
    [ApiController]
    public class EmployeeDepartmentAssignmentController : ControllerBase
    {
        private readonly ILogger<EmployeeDepartmentAssignmentController> _logger;
        private readonly IEmployeeDepartmentAssignmentRepository _repository;

        public EmployeeDepartmentAssignmentController(
            ILogger<EmployeeDepartmentAssignmentController> logger,
            IEmployeeDepartmentAssignmentRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult> GetAssignmentById(int id, CancellationToken cancellationToken)
        {
            var assignment = await _repository.GetById(id, cancellationToken);
            if (assignment is null) return NotFound();
            return Ok(ToResponse(assignment));
        }

        [HttpPost]
        public async Task<ActionResult> CreateAssignment(
            [FromBody] CreateAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            var employee = await _repository.GetEmployee(request.EmployeeId, cancellationToken);
            if (employee is null)
            {
                ModelState.AddModelError(nameof(request.EmployeeId), $"Employee {request.EmployeeId} does not exist.");
                return BadRequestProblem();
            }

            if (!await _repository.DepartmentExists(request.DepartmentId, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.DepartmentId), $"Department {request.DepartmentId} does not exist.");
                return BadRequestProblem();
            }

            if (!IsWithinBr02Window(request.AssignmentDate))
            {
                ModelState.AddModelError(nameof(request.AssignmentDate), "AssignmentDate must not be more than 31 days in the future.");
                return BadRequestProblem();
            }

            if (employee.DepartmentId == request.DepartmentId)
            {
                ModelState.AddModelError(nameof(request.DepartmentId), "An employee cannot be assigned to their own permanent department.");
                return BadRequestProblem();
            }

            var created = await _repository.Create(
                new EmployeeDepartmentAssignment
                {
                    EmployeeId = request.EmployeeId,
                    DepartmentId = request.DepartmentId,
                    AssignmentDate = request.AssignmentDate,
                    Status = AssignmentStatus.Scheduled
                },
                cancellationToken);

            _logger.LogInformation("Created assignment with id {id}", created.AssignmentId);

            return CreatedAtAction(nameof(GetAssignmentById), new { id = created.AssignmentId }, ToResponse(created));
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<ActionResult> UpdateAssignment(
            int id,
            [FromBody] UpdateAssignmentRequest request,
            CancellationToken cancellationToken)
        {
            var existing = await _repository.GetById(id, cancellationToken);
            if (existing is null) return NotFound();

            if (existing.AssignmentDate.Date != request.AssignmentDate.Date
                && !IsWithinBr02Window(request.AssignmentDate))
            {
                ModelState.AddModelError(nameof(request.AssignmentDate), "AssignmentDate must not be more than 31 days in the future.");
                return BadRequestProblem();
            }

            if (request.Status != existing.Status && !IsAllowedTransition(existing.Status, request.Status))
            {
                ModelState.AddModelError(nameof(request.Status), $"Cannot transition from {existing.Status} to {request.Status}.");
                return BadRequestProblem();
            }

            if (request.Status == AssignmentStatus.Active
                && await _repository.HasOtherActiveAssignment(existing.EmployeeId, id, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.Status), "This employee already has another Active assignment.");
                return Conflict(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status409Conflict
                });
            }

            var updated = await _repository.Update(id, request.AssignmentDate, request.Status, cancellationToken);
            if (updated is null) return NotFound();

            _logger.LogInformation("Updated assignment with id {id}", id);

            return Ok(ToResponse(updated));
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<ActionResult> DeleteAssignment(int id, CancellationToken cancellationToken)
        {
            var isDeleted = await _repository.DeleteIfExist(id, cancellationToken);
            if (!isDeleted) return NotFound();

            _logger.LogInformation("Deleted assignment with id {id}", id);
            return NoContent();
        }

        private static bool IsWithinBr02Window(DateTime assignmentDate)
        {
            var latestAllowedDate = DateTime.UtcNow.Date.AddDays(31);
            return assignmentDate.Date <= latestAllowedDate;
        }

        private static bool IsAllowedTransition(AssignmentStatus from, AssignmentStatus to) => from switch
        {
            AssignmentStatus.Scheduled => to is AssignmentStatus.Active or AssignmentStatus.Cancelled,
            AssignmentStatus.Active => to is AssignmentStatus.Completed or AssignmentStatus.Cancelled,
            AssignmentStatus.Completed => false,
            AssignmentStatus.Cancelled => false,
            _ => false
        };

        private ActionResult BadRequestProblem() =>
            BadRequest(new ValidationProblemDetails(ModelState)
            {
                Status = StatusCodes.Status400BadRequest
            });

        private static AssignmentResponse ToResponse(EmployeeDepartmentAssignment assignment) =>
            new(assignment.AssignmentId, assignment.EmployeeId, assignment.DepartmentId, assignment.AssignmentDate, assignment.Status);
    }
}