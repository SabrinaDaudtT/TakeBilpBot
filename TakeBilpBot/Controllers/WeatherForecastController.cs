using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TakeBilpBot.Model;

namespace MetaInspector.Controllers
{
    [Route("")]
    public class WeatherForecastController : Controller
    {
        // GET api/values
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public async Task<TakeDados> Get([FromQuery] string url, CancellationToken cancellationToken = default)
        {
            url = url.Trim();
            url = AddHttpUrl(url);

            var response = await TryGetResponseAsync(url, cancellationToken);
            if (response == null)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var charset = doc.DocumentNode.SelectSingleNode("//meta[@charset]")?.Attributes["charset"]?.Value ?? string.Empty;
            try
            {
                var encoding = Encoding.GetEncoding(charset);
                if (encoding != Encoding.UTF8)
                {
                    var contentBytes = await response.Content.ReadAsByteArrayAsync();
                    content = encoding.GetString(contentBytes);
                    doc.LoadHtml(content);
                }
            }
            catch (Exception e) { }

            var metaTags = doc.DocumentNode.SelectNodes("//meta");
            var takeDados = new TakeDados();

            if (metaTags == null) return null;

            foreach (var tag in metaTags)
            {
                var tagName = tag.Attributes["name"]?.Value.ToLower();
                var tagContent = tag.Attributes["content"];
                var tagProperty = tag.Attributes["property"]?.Value.ToLower();
                if (string.IsNullOrEmpty(takeDados.Titulo) && (tagName == "title" || tagName == "twitter:title" || tagProperty == "og:title"))
                {
                    takeDados.Titulo = tagContent.Value.Trim();
                }
                else if (string.IsNullOrEmpty(takeDados.Descriacao) && (tagName == "description" || tagName == "twitter:description" || tagProperty == "og:description"))
                {
                    takeDados.Descriacao = tagContent.Value.Trim();
                }
                else if (string.IsNullOrEmpty(takeDados.Imagem) && (tagName == "twitter:image" || tagProperty == "og:image"))
                {
                    takeDados.Imagem = tagContent.Value.Trim();
                }
            }

            // if no metadata title, get title
            if (string.IsNullOrEmpty(takeDados.Titulo))
            {
                var title = doc.DocumentNode.SelectSingleNode("//title");
                takeDados.Titulo = title?.InnerText;
            }
            // If using local path
            if (takeDados.Imagem.StartsWith('/') && Uri.TryCreate(url, UriKind.Absolute, out var result))
            {
                takeDados.Imagem = result.Scheme + "://" + result.Host + takeDados.Imagem;
            }
            return takeDados;
        }

        private async Task<HttpResponseMessage> TryGetResponseAsync(string url, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var result))
                {
                    var response = await client.GetAsync(result, cancellationToken);
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399)
                    {
                        response = await client.GetAsync(response.Headers.Location, cancellationToken);
                    }
                    return response;
                }
            }
            return null;
        }

        private string AddHttpUrl(string url)
        {
            url = url.Replace("https://", "http://");
            return url.StartsWith("http://") ? url : $"http://{url}";
        }
    }
}