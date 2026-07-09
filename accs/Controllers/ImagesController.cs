using accs.Models;
using accs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImagesController> _logger;
        private readonly RewardService _rewardService;
        private readonly RankService _rankService;
        private readonly UnitService _unitService;
        private readonly string[] _allowedExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

        public ImagesController(IWebHostEnvironment environment, ILogger<ImagesController> logger, RewardService rewardService, RankService rankService, UnitService unitService)
        {
            _environment = environment;
            _logger = logger;
            _rewardService = rewardService;
            _rankService = rankService;
            _unitService = unitService;
        }


        [HttpPost("rewards/{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UploadRewardImage([FromRoute] int rewardId, [FromForm] IFormFile file)
        {
            try
            {
                var actor = HttpContext.Items["Actor"] as Unit;
                if (actor == null)
                {
                    return Unauthorized(new { error = "Пользователь не идентифицирован." });
                }

                _rewardService.Actor = actor;
                var permissionCheck = await _rewardService.CheckCanManageRewards();
                if (!permissionCheck.IsSuccess)
                {
                    return StatusCode(403, new { error = permissionCheck.Message });
                }

                var rewardExistCheck = await _rewardService.GetAsync(rewardId);
                if (!rewardExistCheck.IsSuccess || rewardExistCheck.Value == null)
                {
                    return NotFound(new { error = $"Награда с ID {rewardId} не существует. Некуда привязать картинку." });
                }

                string folderName = rewardExistCheck.Value.GetFolderName();
                return await SaveImageAsync(folderName, rewardId, file);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UploadRewardImage: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost("ranks/{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UploadRankImage([FromRoute] int id, [FromForm] IFormFile file)
        {
            try
            {
                var actor = HttpContext.Items["Actor"] as Unit;
                if (actor == null)
                {
                    return Unauthorized(new { error = "Пользователь не идентифицирован." });
                }

                _rankService.Actor = actor;
                var permissionCheck = await _rankService.CheckCanManageAsync();
                if (!permissionCheck.IsSuccess)
                {
                    return StatusCode(403, new { error = permissionCheck.Message });
                }

                var rankExistCheck = await _rankService.GetAsync(id);
                if (!rankExistCheck.IsSuccess || rankExistCheck.Value == null)
                {
                    return NotFound(new { error = $"Ранг с ID {id} не существует. Некуда привязать картинку." });
                }

                string folderName = rankExistCheck.Value.GetFolderName();
                return await SaveImageAsync(folderName, id, file);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UploadRankImage: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        [HttpPost("backgrounds/{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UploadBackgroundImage([FromRoute] int id, [FromForm] IFormFile file)
        {
            try
            {
                var actor = HttpContext.Items["Actor"] as Unit;
                if (actor == null)
                {
                    return Unauthorized(new { error = "Пользователь не идентифицирован." });
                }

                _unitService.Actor = actor;
                var unitExistCheck = await _unitService.GetAsync(actor.DiscordId);
                if (!unitExistCheck.IsSuccess || unitExistCheck.Value == null)
                {
                    return NotFound(new { error = $"Пользователь с ID {id} не существует. Некуда привязать картинку." });
                }

                BackgroundPicture picture = new BackgroundPicture();
                string folderName = picture.GetFolderName();
                return await SaveImageAsync(folderName, id, file);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UploadBackgroundImage: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }



        [HttpPost("kits/{rewardId}")]
        [Authorize]
        public async Task<IActionResult> UploadKitImage([FromRoute] int id, [FromForm] IFormFile file)
        {
            try
            {
                var actor = HttpContext.Items["Actor"] as Unit;
                if (actor == null)
                {
                    return Unauthorized(new { error = "Пользователь не идентифицирован." });
                }

                _unitService.Actor = actor;
                var unitExistCheck = await _unitService.GetAsync(actor.DiscordId);
                if (!unitExistCheck.IsSuccess || unitExistCheck.Value == null)
                {
                    return NotFound(new { error = $"Пользователь с ID {id} не существует. Некуда привязать картинку." });
                }

                FavoriteKit kit = new FavoriteKit();
                string folderName = kit.GetFolderName();
                return await SaveImageAsync(folderName, id, file);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UploadKitImage: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }


        private async Task<IActionResult> SaveImageAsync(string folderName, int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Файл не передан или пуст." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerSuffix();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = $"Недопустимый формат файла. Разрешены: {string.Join(", ", _allowedExtensions)}" });
            }

            var targetFolder = Path.Combine(_environment.WebRootPath, folderName);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var directoryInfo = new DirectoryInfo(targetFolder);
            var existingFiles = directoryInfo.GetFiles($"{id}.*");
            foreach (var existingFile in existingFiles)
            {
                existingFile.Delete();
            }

            var fileName = $"{id}{extension}";
            var fullPath = Path.Combine(targetFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"Картинка успешно сохранена: {fullPath}");
            return Ok(new { message = "Изображение успешно загружено", fileName = fileName });
        }
    }


    public static class StringExtensions
    {
        public static string ToLowerSuffix(this string str) => str?.ToLower().Trim() ?? string.Empty;
    }
}
