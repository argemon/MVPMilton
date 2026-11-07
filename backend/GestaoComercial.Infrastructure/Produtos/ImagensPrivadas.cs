using System.Text.RegularExpressions;
using GestaoComercial.Application.Clientes;
using SkiaSharp;
namespace GestaoComercial.Infrastructure.Produtos;

public sealed class ImagensPrivadas(string root)
{
    public string Root { get; }=Path.GetFullPath(root);
    public const int MaxBytes=2097152;
    public string Caminho(string key) {
        if(!Regex.IsMatch(key,"^[a-f0-9]{32}\\.png$")) throw new CadastroException("IMAGEM_INVALIDA");
        return Path.Combine(Root,key);
    }
    public static byte[] Validar(byte[] content) {
        if(content.Length>MaxBytes) throw new CadastroException("IMAGEM_EXCEDE_LIMITE",413);
        if(content.Length==0) throw new CadastroException("IMAGEM_INVALIDA");
        // Detect signatures before native decode; never trust a client filename or MIME.
        var png=content.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10});
        var jpeg=content.Length>=3&&content[0]==255&&content[1]==216&&content[2]==255;
        if(!png&&!jpeg) throw new CadastroException("TIPO_IMAGEM_INVALIDO",415);
        using var stream=new SKMemoryStream(content);
        using var codec=SKCodec.Create(stream);
        if(codec is null) throw new CadastroException("IMAGEM_INVALIDA");
        if(codec.EncodedFormat is not SKEncodedImageFormat.Png and not SKEncodedImageFormat.Jpeg) throw new CadastroException("TIPO_IMAGEM_INVALIDO",415);
        var info=codec.Info;
        if(info.Width is <1 or >4096||info.Height is <1 or >4096||codec.FrameCount>1) throw new CadastroException("DIMENSOES_IMAGEM_INVALIDAS");
        using var bitmap=new SKBitmap(new SKImageInfo(info.Width,info.Height,SKColorType.Rgba8888,SKAlphaType.Premul));
        if(codec.GetPixels(bitmap.Info,bitmap.GetPixels())!=SKCodecResult.Success) throw new CadastroException("IMAGEM_INVALIDA");
        using var image=SKImage.FromBitmap(bitmap); using var data=image.Encode(SKEncodedImageFormat.Png,100);
        if(data is null||data.Size>MaxBytes) throw new CadastroException("IMAGEM_EXCEDE_LIMITE",413);
        return data.ToArray();
    }
    public async Task<ImagemDocumento> GravarAsync(byte[] content,CancellationToken ct) {
        var bytes=Validar(content); Directory.CreateDirectory(Root);
        var key=Guid.NewGuid().ToString("N")+".png";
        await using(var file=new FileStream(Caminho(key),FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)) await file.WriteAsync(bytes,ct);
        return new(){Chave=key,TamanhoBytes=bytes.Length};
    }
    public void RemoverAposCommit(string? key) {
        if(key is null) return;
        try { File.Delete(Caminho(key)); } catch(IOException) { /* Orphan retained for explicit maintenance cleanup. */ } catch(UnauthorizedAccessException) { }
    }
}
