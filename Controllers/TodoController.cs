using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeApi.Core.Todo;
using PracticeApi.Domain.DTO;
using PracticeApi.Persistent;

namespace PracticeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly TodoService _todoService;
        private readonly DataContext _context;

        // Combine both constructors into one
        public TodoController(TodoService todoService, DataContext context)
        {
            _todoService = todoService;
            _context = context;
        }

        // Create a new Todo
        [HttpPost("createTodos")]
        [Authorize]
        public async Task<IActionResult> CreateTodo([FromBody] TodoDto todoCreateRequest)
        {
            var response = await _todoService.CreateTodo(todoCreateRequest);

            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response);
        }

        // Get all Todos
        [HttpGet("getTodos")]
        public async Task<PagedResponse<List<TodoResponseDto>>> GetTodos(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string title = null,
            [FromQuery] bool? isCompleted = null)
        {
            // Ensure that pageSize is greater than 0, otherwise set it to a default value
            if (pageSize <= 0)
            {
                pageSize = 10; // Default page size
            }

            var todosQuery = _context.Todos.AsQueryable();

            // Apply title filter if provided using EF.Functions.Like for case-insensitive search
            if (!string.IsNullOrEmpty(title))
            {
                todosQuery = todosQuery.Where(t => EF.Functions.Like(t.Title, $"%{title}%"));
            }

            // Apply isCompleted filter if provided
            if (isCompleted.HasValue)
            {
                todosQuery = todosQuery.Where(t => t.IscomPleted == isCompleted.Value);
            }

            // Get the total number of records after applying filters
            var totalRecords = await todosQuery.CountAsync();

            // Execute the query with pagination
            var todos = await todosQuery
                .Select(t => new TodoResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    IscomPleted = t.IscomPleted,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    DueDate = t.DueDate
                })
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<List<TodoResponseDto>>(todos, pageNumber, pageSize, totalRecords, "Todos retrieved successfully.");
        }


        // Get Todo by Id
        [HttpGet("get-todo-by-id/{todoId}")]
        [Authorize]
        public async Task<IActionResult> GetTodoById(string todoId)
        {
            var response = await _todoService.GetTodoById(todoId);

            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response);
        }

        // Update Todo by Id - Mark as Completed
        [HttpPut("update-todo/{todoId}")]
        [Authorize]
        public async Task<IActionResult> UpdateTodo(string todoId, [FromBody] TodoUpdateRequest todoUpdateRequest)
        {
            var response = await _todoService.UpdateTodo(todoId, todoUpdateRequest);

            if (response.Succeeded)
            {
                return Ok(response);
            }

            return StatusCode(response.Code, response);
        }

        // Delete Todo by Id
        [HttpDelete("delete-todo/{todoId}")]
        [Authorize]
        public async Task<IActionResult> DeleteTodo(string todoId)
        {
            var response = await _todoService.DeleteTodo(todoId);

            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response
        }

        // Get Overdue Todos
        [HttpGet("get-overdue-todos")]
        [Authorize]
        public async Task<IActionResult> GetOverdueTodos()
        {
            var response = await _todoService.GetOverdueTodos();

            if (response.Succeeded)
            {
                return Ok(response); // Return success response
            }

            return StatusCode(response.Code, response); // Return error response
        }
    }
}
