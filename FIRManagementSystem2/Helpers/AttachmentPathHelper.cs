using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace FIRManagementSystem2.Helpers
{
    public static class AttachmentPathHelper
    {
        private const string AttachmentsMarker = "Attachments";

        public static string GetAttachmentRoot(HttpServerUtilityBase server)
        {
            var configured = ConfigurationManager.AppSettings["AttachmentRoot"];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.TrimEnd('\\', '/');

            return server.MapPath("~/Attachments");
        }

        public static string BuildRelativePath(int firId, string fileName)
        {
            return Path.Combine("FIR", firId.ToString(), fileName);
        }

        public static string ResolveFullPath(HttpServerUtilityBase server, string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return null;

            if (File.Exists(storedPath))
                return storedPath;

            var root = GetAttachmentRoot(server);
            var relative = ToRelativePath(storedPath);
            var resolved = Path.Combine(root, relative);

            var found = TryExistingFile(resolved);
            if (found != null)
                return found;

            if (Path.HasExtension(resolved))
            {
                found = TryExistingFile(Path.Combine(Path.GetDirectoryName(resolved), Path.GetFileNameWithoutExtension(resolved)));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static string TryExistingFile(string path)
        {
            if (File.Exists(path))
                return path;

            if (!Path.HasExtension(path))
            {
                foreach (var ext in new[] { ".jpeg", ".jpg", ".png", ".gif", ".pdf", ".doc", ".docx" })
                {
                    var withExt = path + ext;
                    if (File.Exists(withExt))
                        return withExt;
                }
            }

            return null;
        }

        private static string ToRelativePath(string storedPath)
        {
            if (!Path.IsPathRooted(storedPath))
                return storedPath.Replace('/', Path.DirectorySeparatorChar);

            var markerIndex = storedPath.IndexOf(AttachmentsMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var afterMarker = storedPath.Substring(markerIndex + AttachmentsMarker.Length)
                    .TrimStart('\\', '/');
                return afterMarker.Replace('/', Path.DirectorySeparatorChar);
            }

            return storedPath;
        }
    }
}
