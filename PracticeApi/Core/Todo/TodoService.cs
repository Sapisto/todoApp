using Microsoft.EntityFrameworkCore;
using PracticeApi.Domain.DTO;
using PracticeApi.Domain.Entities;
using PracticeApi.DTO.UserDTOs;
using PracticeApi.Persistent;
using System;

namespace PracticeApi.Core.Todo
{
    public class TodoService
    {
        private readonly DataContext _context;

        // Constructor
        public TodoService(DataContext context)
        {
            _context = context;
        }

        // Create Todo
        public async Task<GeneralResponse<TodoResponseDto>> CreateTodo(TodoDto todoDto)
        {
            // Force IscomPleted to false at this stage
            todoDto.IscomPleted = false;

            // Validation checks
            //if (string.IsNullOrEmpty(todoDto.Title))
            //{
            //    return new GeneralResponse<TodoResponseDto>("Title is required.", 400, false);
            //}

            //if (string.IsNullOrEmpty(todoDto.Description))
            //{
            //    return new GeneralResponse<TodoResponseDto>("Description is required.", 400, false);
            //}

            // Check if a Todo with the same title already exists
            var existingTodo = await _context.Todos.FirstOrDefaultAsync(t => t.Title == todoDto.Title);
            if (existingTodo != null)
            {
                return new GeneralResponse<TodoResponseDto>("A Todo with the same title already exists.", 409, false);
            }

            // Create the Todo entity
            var newTodo = new Domain.Entities.Todo
            {
                Id = Guid.NewGuid().ToString(),
                Title = todoDto.Title,
                Description = todoDto.Description,
                IscomPleted = todoDto.IscomPleted, // Defaults to false here
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            // Save to the database
            await _context.Todos.AddAsync(newTodo);
            await _context.SaveChangesAsync();

            // Map the entity to a response DTO
            var todoResponse = new TodoResponseDto
            {
                Id = newTodo.Id,
                Title = newTodo.Title,
                Description = newTodo.Description,
                IscomPleted = newTodo.IscomPleted,
                CreatedAt = newTodo.CreatedAt,
                DueDate = newTodo.DueDate
            };

            return new GeneralResponse<TodoResponseDto>(todoResponse, "Todo created successfully.");
        }

        // Get all Todos (Paginated)
        public async Task<PagedResponse<List<TodoResponseDto>>> GetTodos(int pageNumber, int pageSize, string title = null, bool? isCompleted = null)
        {
            var todosQuery = _context.Todos.AsQueryable();

            // Filter by title if it's provided
            if (!string.IsNullOrEmpty(title))
            {
                todosQuery = todosQuery.Where(t => t.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by isCompleted if it's provided
            if (isCompleted.HasValue)
            {
                todosQuery = todosQuery.Where(t => t.IscomPleted == isCompleted.Value);
            }

            // Get the total number of records after the filters have been applied
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
        public async Task<GeneralResponse<TodoResponseDto>> GetTodoById(string todoId)
        {
            if (string.IsNullOrEmpty(todoId))
            {
                return new GeneralResponse<TodoResponseDto>("Todo Id is required.", 400, false);
            }

            var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == todoId);
            if (todo == null)
            {
                return new GeneralResponse<TodoResponseDto>("Todo not found.", 404, false);
            }

            var todoResponse = new TodoResponseDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                IscomPleted = todo.IscomPleted,
                CreatedAt = todo.CreatedAt,
                CompletedAt = todo.CompletedAt,
                DueDate = todo.DueDate
            };

            return new GeneralResponse<TodoResponseDto>(todoResponse, "Todo retrieved successfully.");
        }

        // Update Todo - Mark as Completed or Modify Other Fields
        public async Task<GeneralResponse<TodoResponseDto>> UpdateTodo(string todoId, TodoUpdateRequest todoUpdateRequest)
        {
            if (string.IsNullOrEmpty(todoId))
            {
                return new GeneralResponse<TodoResponseDto>("Todo Id is required.", 400, false);
            }

            var existingTodo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == todoId);
            if (existingTodo == null)
            {
                return new GeneralResponse<TodoResponseDto>("Todo not found.", 404, false);
            }

            // Update fields
            existingTodo.Title = todoUpdateRequest.Title ?? existingTodo.Title;
            existingTodo.Description = todoUpdateRequest.Description ?? existingTodo.Description;

            // Handle IsCompleted logic internally
            if (todoUpdateRequest.IsCompleted.HasValue && todoUpdateRequest.IsCompleted.Value)
            {
                if (!existingTodo.IscomPleted) // Only mark as completed if not already completed
                {
                    existingTodo.IscomPleted = true;
                    existingTodo.CompletedAt = DateTime.UtcNow; // Set the completion timestamp
                }
            }
            else
            {
                existingTodo.IscomPleted = false; // Reset IsCompleted
                existingTodo.CompletedAt = null;  // Reset CompletedAt
            }

            _context.Todos.Update(existingTodo);
            await _context.SaveChangesAsync();

            // Map to response DTO
            var todoResponse = new TodoResponseDto
            {
                Id = existingTodo.Id,
                Title = existingTodo.Title,
                Description = existingTodo.Description,
                IscomPleted = existingTodo.IscomPleted,
                CreatedAt = existingTodo.CreatedAt,
                CompletedAt = existingTodo.CompletedAt,
                DueDate = existingTodo.DueDate // Note: DueDate is not updated here
            };

            return new GeneralResponse<TodoResponseDto>(todoResponse, "Todo updated successfully.");
        }




        // Delete Todo
        public async Task<GeneralResponse<object>> DeleteTodo(string todoId)
        {
            if (string.IsNullOrEmpty(todoId))
            {
                return new GeneralResponse<object>("Todo Id is required.", 400, false);
            }

            var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == todoId);
            if (todo == null)
            {
                return new GeneralResponse<object>("Todo not found.", 404, false);
            }

            _context.Todos.Remove(todo);
            await _context.SaveChangesAsync();

            return new GeneralResponse<object>("Todo deleted successfully.");
        }

        // Get Overdue Todos
        public async Task<GeneralResponse<List<TodoResponseDto>>> GetOverdueTodos()
        {
            var overdueTodos = await _context.Todos
                .Where(t => t.DueDate < DateTime.UtcNow && !t.IscomPleted)
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
                .ToListAsync();

            return new GeneralResponse<List<TodoResponseDto>>(overdueTodos, "Overdue todos retrieved successfully.");
        }
    }
}
