namespace Projetar.Web.Services;

/// <summary>Diff de palavras entre duas versões de um corpo HTML, pra destacar o que mudou (tipo redline de emenda legislativa).</summary>
public interface IRevisaoDiffService
{
    string RenderRedline(string corpoAntigoHtml, string corpoNovoHtml);
}
