using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Reflection;

namespace OHotel.NETCoreMVC.Controllers.Sys
{
    [Area("Sys")]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ToolsController : ControllerBase
    {
        /* 01. 建立 {folderName} 資料夾至 Views 裡 */
        [HttpPost("{folderName}")]
        public IActionResult CreateFolder(string folderName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 建立新資料夾的完整路徑
                string newFolderPath = Path.Combine(currentWorkingDirectory, "Views", folderName);

                // 檢查新資料夾是否已經存在
                if (Directory.Exists(newFolderPath))
                {
                    return Conflict($"資料夾 '{folderName}' 已經存在。");
                }

                // 建立新資料夾
                Directory.CreateDirectory(newFolderPath);

                return Ok($"成功建立資料夾 '{folderName}'。");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"建立資料夾時發生錯誤: {ex.Message}");
            }
        }

        /* 02. 建立 `{folderName}Controller.cs` 至 Controllers 裡 */
        [HttpPost("{folderName}")]
        public IActionResult CreateControllerFile(string folderName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 取得專案名稱
                string projectName = Assembly.GetEntryAssembly()?.GetName().Name ?? "OHotel.NETCoreMVC";

                // 資料夾的完整路徑
                string folderPath = Path.Combine(currentWorkingDirectory, "Controllers");

                // 建立新檔案的完整路徑
                string filePath = Path.Combine(folderPath, $"{folderName}Controller.cs");

                // 檢查檔案是否已經存在
                if (System.IO.File.Exists(filePath))
                {
                    return Conflict($"檔案 '{folderName}Controller.cs' 已經存在。");
                }

                // 建立新的 Controller 檔案
                using (StreamWriter writer = System.IO.File.CreateText(filePath))
                {
                    writer.WriteLine("using Microsoft.AspNetCore.Mvc;");
                    writer.WriteLine($"using {projectName}.Models;");
                    writer.WriteLine("using System.Diagnostics;");
                    writer.WriteLine();
                    writer.WriteLine($"namespace {projectName}.Controllers");
                    writer.WriteLine("{");
                    writer.WriteLine($"    public class {folderName}Controller : Controller");
                    writer.WriteLine("    {");
                    writer.WriteLine("        // 以下註解如果後續要增加Action方便複製，請勿刪除");
                    writer.WriteLine("        //public IActionResult YourAction()");
                    writer.WriteLine("        //{");
                    writer.WriteLine("        //    return View();");
                    writer.WriteLine("        //}");
                    writer.WriteLine();
                    writer.WriteLine("        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]");
                    writer.WriteLine("        public IActionResult Error()");
                    writer.WriteLine("        {");
                    writer.WriteLine("            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });");
                    writer.WriteLine("        }");
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }

                return Ok($"成功建立檔案 '{folderName}Controller.cs'。");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"建立檔案時發生錯誤: {ex.Message}");
            }
        }

        /* 03. 至 Views 找到相對應的{oldFolderName}資料夾，使用{newFolderName}修改資料夾名稱 */
        [HttpPatch("{oldFolderName}/{newFolderName}")]
        public IActionResult EditFolderName(string oldFolderName, string newFolderName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 舊資料夾的完整路徑
                string oldFolderPath = Path.Combine(currentWorkingDirectory, "Views", oldFolderName);

                // 新資料夾的完整路徑
                string newFolderPath = Path.Combine(currentWorkingDirectory, "Views", newFolderName);

                // 檢查舊資料夾是否存在
                if (!Directory.Exists(oldFolderPath))
                {
                    return NotFound($"找不到資料夾 '{oldFolderName}'。");
                }

                // 檢查新資料夾是否已經存在
                if (Directory.Exists(newFolderPath))
                {
                    return Conflict($"資料夾 '{newFolderName}' 已經存在。");
                }

                // 更改資料夾名稱
                Directory.Move(oldFolderPath, newFolderPath);

                return Ok($"成功變更資料夾名稱，'{oldFolderName}' 變更為 '{newFolderName}'。");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"變更資料夾名稱時發生錯誤: {ex.Message}");
            }
        }

        /* 04. 至 Views 檢查是有存在{folderName}資料夾回傳 boolean 值 */
        [HttpGet("{folderName}")]
        public IActionResult CheckFolderExists(string folderName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 資料夾的完整路徑
                string folderPath = Path.Combine(currentWorkingDirectory, "Views", folderName);

                // 檢查資料夾是否存在
                bool folderExists = Directory.Exists(folderPath);

                return Ok(folderExists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"檢查資料夾時發生錯誤: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------------------------------------
        /* 05. 至 Views 裡 {folderName} 建立 {fileName}.cshtml 檔案 */
        [HttpPost("{folderName}/{fileName}")]
        public IActionResult CreateCshtmlFile(string folderName, string fileName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 資料夾的完整路徑
                string folderPath = Path.Combine(currentWorkingDirectory, "Views", folderName);

                // 檢查資料夾是否存在
                if (!Directory.Exists(folderPath))
                {
                    return NotFound($"找不到資料夾 '{folderName}'。");
                }

                // 建立檔案的完整路徑
                string filePath = Path.Combine(folderPath, $"{fileName}.cshtml");

                // 檢查檔案是否已經存在
                if (System.IO.File.Exists(filePath))
                {
                    return Conflict($"檔案 '{fileName}.cshtml' 已經存在。");
                }

                // 建立新的 cshtml 檔案
                using (StreamWriter writer = System.IO.File.CreateText(filePath))
                {
                    writer.WriteLine("@{");
                    writer.WriteLine("    ViewData[\"Title\"] = \"My Page Title\";");
                    writer.WriteLine("}");
                    writer.WriteLine("<h1>Welcome to My Page</h1>");
                }

                return Ok($"成功建立檔案 '{fileName}.cshtml'。");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"建立檔案時發生錯誤: {ex.Message}");
            }
        }

        /* 06. 至`{folderName}Controller.cs`裡面增加新的{fileName} View Action */
        [HttpPost("{folderName}/{fileName}")]
        public IActionResult AddViewAction(string folderName, string fileName)
        {
            try
            {
                // 取得目前工作目錄的路徑
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                // 資料夾的完整路徑
                string folderPath = Path.Combine(currentWorkingDirectory, "Controllers");

                // 檢查資料夾是否存在
                if (!Directory.Exists(folderPath))
                {
                    return NotFound($"找不到資料夾 '{folderName}'。");
                }

                // 建立檔案的完整路徑
                string filePath = Path.Combine(folderPath, $"{folderName}Controller.cs");

                // 檢查檔案是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"找不到檔案 '{folderName}Controller.cs'。");
                }

                // 讀取檔案內容
                string[] lines = System.IO.File.ReadAllLines(filePath);

                // 檢查是否已存在指定的 Action
                string actionName = $"{fileName}()";
                if (lines.Contains($"public IActionResult {actionName}"))
                {
                    return Conflict($"Action '{actionName}' 已存在於檔案 '{folderName}Controller.cs'。");
                }

                // 尋找插入位置
                int insertIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    // 找到第一個以 "}" 結尾的行
                    if (lines[i].TrimEnd().EndsWith("}"))
                    {
                        insertIndex = i;
                        break;
                    }
                }

                // 如果找不到插入位置，則回傳錯誤
                if (insertIndex == -1)
                {
                    return StatusCode(500, $"無法找到插入位置於檔案 '{folderName}Controller.cs'。");
                }

                // 在指定位置插入新的 Action
                List<string> updatedLines = new List<string>(lines);
                updatedLines.Insert(insertIndex + 1, "        }");
                updatedLines.Insert(insertIndex + 1, "            return View();");
                updatedLines.Insert(insertIndex + 1, "        {");
                updatedLines.Insert(insertIndex + 1, $"        public IActionResult {actionName}");

                // 覆寫檔案內容
                System.IO.File.WriteAllLines(filePath, updatedLines);

                return Ok($"成功新增 Action '{actionName}' 於檔案 '{folderName}Controller.cs'。");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"發生錯誤: {ex.Message}");
            }
        }

    }
}
