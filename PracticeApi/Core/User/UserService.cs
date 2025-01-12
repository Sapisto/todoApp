using PracticeApi.DTO.UserDTOs;
using PracticeApi.Persistent;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PracticeApi.Domain.DTO;

namespace PracticeApi.Core.UserServices
{
    public class UserService
    {
        private readonly DataContext _context;
        private readonly string _jwtSecret;

        // Constructor accepts secret for JWT token
        public UserService(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _jwtSecret = configuration.GetValue<string>("JWT:Secret");
        }

        // Register new user
        public async Task<GeneralResponse<UserRegistrationResponse>> CreateUser(UserRegistrationRequest userRegistrationRequest)
        {
            // Check for required fields
            if (string.IsNullOrEmpty(userRegistrationRequest.Name) ||
                string.IsNullOrEmpty(userRegistrationRequest.Email) ||
                string.IsNullOrEmpty(userRegistrationRequest.Password) ||
                string.IsNullOrEmpty(userRegistrationRequest.UserName) ||
                string.IsNullOrEmpty(userRegistrationRequest.PhoneNumber))
            {
                return new GeneralResponse<UserRegistrationResponse>("All fields are required.", 400, false);
            }

            // Check if passwords match
            if (userRegistrationRequest.Password != userRegistrationRequest.ConfirmPassword)
            {
                return new GeneralResponse<UserRegistrationResponse>("Passwords do not match.", 400, false);
            }

            // Check if email is already in use
            var existingEmailUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userRegistrationRequest.Email);
            if (existingEmailUser != null)
            {
                return new GeneralResponse<UserRegistrationResponse>("Email already in use.", 409, false);
            }

            // Check if username is already in use
            var existingUsernameUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userRegistrationRequest.UserName);
            if (existingUsernameUser != null)
            {
                return new GeneralResponse<UserRegistrationResponse>("Username already in use.", 409, false);
            }

            // Hash the password
            string hashedPassword = HashPassword(userRegistrationRequest.Password);

            // Create new user
            var newUser = new Domain.Entities.User
            {
                Name = userRegistrationRequest.Name,
                UserName = userRegistrationRequest.UserName,
                Email = userRegistrationRequest.Email,
                PhoneNumber = userRegistrationRequest.PhoneNumber,
                Password = hashedPassword,
                Id = Guid.NewGuid().ToString()
            };

            // Save user to the database
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Map the saved user to UserResponseDto to exclude the password from the response
            var userResponse = new UserRegistrationResponse
            {
                Id = newUser.Id,
                Name = newUser.Name,
                UserName = newUser.UserName,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber
            };

            return new GeneralResponse<UserRegistrationResponse>(userResponse, "User registered successfully.");
        }



        // Get all users
        public async Task<PagedResponse<List<GetAllUsersDto>>> GetAllUsers(int pageNumber, int pageSize)
        {
            var usersQuery = _context.Users.Select(u => new GetAllUsersDto
            {
                Id = u.Id,
                Name = u.Name,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber
            });

            var totalRecords = await usersQuery.CountAsync();
            var users = await usersQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResponse<List<GetAllUsersDto>>(users, pageNumber, pageSize, totalRecords, "Users retrieved successfully.");
        }

        // Get user by Id
        public async Task<GeneralResponse<GetAllUsersDto>> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<GetAllUsersDto>("User Id is required.", 400, false);
            }

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new GetAllUsersDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new GeneralResponse<GetAllUsersDto>("User not found.", 404, false);
            }

            return new GeneralResponse<GetAllUsersDto>(user, "User retrieved successfully.");
        }


        // Login user
        public async Task<GeneralResponse<object>> LoginUser(UserLoginRequest userLoginRequest)
        {
            if (string.IsNullOrEmpty(userLoginRequest.UserName) || string.IsNullOrEmpty(userLoginRequest.Password))
            {
                return new GeneralResponse<object>("Username and password are required.", 400, false);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userLoginRequest.UserName);
            if (existingUser == null)
            {
                return new GeneralResponse<object>("User not found.", 404, false);
            }

            var hashedPassword = HashPassword(userLoginRequest.Password);
            if (existingUser.Password != hashedPassword)
            {
                return new GeneralResponse<object>("Invalid credentials.", 401, false);
            }

            // Generate JWT Token
            var token = GenerateJwtToken(existingUser);

            var result = new
            {
                User = new
                {
                    existingUser.Id,
                    existingUser.Name,
                    existingUser.UserName,
                    existingUser.Email,
                    existingUser.PhoneNumber
                },
                Token = token
            };

            return new GeneralResponse<object>(result, "User logged in successfully.");
        }
        // Update user details by Id
        public async Task<GeneralResponse<UserRegistrationResponse>> UpdateUser(string userId, UserUpdateRequest userUpdateRequest)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<UserRegistrationResponse>("User Id is required.", 400, false);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
            {
                return new GeneralResponse<UserRegistrationResponse>("User not found.", 404, false);
            }

            // Update user details
            existingUser.Name = userUpdateRequest.Name ?? existingUser.Name;
            existingUser.Email = userUpdateRequest.Email ?? existingUser.Email;
            existingUser.PhoneNumber = userUpdateRequest.PhoneNumber ?? existingUser.PhoneNumber;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            // Map to response DTO
            var updatedUserResponse = new UserRegistrationResponse
            {
                Id = existingUser.Id,
                Name = existingUser.Name,
                UserName = existingUser.UserName,
                Email = existingUser.Email,
                PhoneNumber = existingUser.PhoneNumber
            };

            return new GeneralResponse<UserRegistrationResponse>(updatedUserResponse, "User updated successfully.");
        }

        // UserService class - adding DeleteUser method
        public async Task<GeneralResponse<object>> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new GeneralResponse<object>("User Id is required.", 400, false);
            }

            // Find the user in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new GeneralResponse<object>("User not found.", 404, false);
            }

            // Remove user from the database
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return new GeneralResponse<object>("User deleted successfully.");
        }


        // Hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }


        // Generate JWT token
        private string GenerateJwtToken(Domain.Entities.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "Acube",
                audience: "Acube",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), //Token expiration
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
