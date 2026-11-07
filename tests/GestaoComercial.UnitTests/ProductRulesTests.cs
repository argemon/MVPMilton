extern alias ApiHost;
using GestaoComercial.Domain.Cadastros;
using GestaoComercial.Infrastructure.Produtos;
using GestaoComercial.Application.Clientes;
using SkiaSharp;
using Xunit;

public sealed class ProductRulesTests
{
    [Fact] public void NormalizesSkuAndPlainTextAndExactMoney(){var p=DadosProduto.Criar(" abc-1 "," Produto   Teste "," texto ",Guid.NewGuid(),null,1250,0);Assert.Equal("ABC-1",p.Sku);Assert.Equal("Produto Teste",p.Nome);Assert.Equal(1250,ApiHost::GestaoComercial.Api.Acesso.DinheiroHttp.Centavos(12.50m));Assert.Equal("Categoria Teste",DadosProduto.NomeReferencia(" Categoria   Teste "));}
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1000000000)]
    public void RejectsInvalidPrice(long cents)=>Assert.Throws<ArgumentException>(()=>DadosProduto.Criar("SKU","Produto",null,Guid.NewGuid(),null,cents,0));
    [Theory] [InlineData("0")] [InlineData("12.505")] [InlineData("10000000")]
    public void HttpRejectsInvalidScaleAndBounds(string input)=>Assert.Throws<CadastroException>(()=>ApiHost::GestaoComercial.Api.Acesso.DinheiroHttp.Centavos(decimal.Parse(input,System.Globalization.CultureInfo.InvariantCulture)));
    [Theory] [InlineData("bad sku","Produto",null,0)] [InlineData("SKU","x",null,0)] [InlineData("SKU","Produto","<script>",0)] [InlineData("SKU","Produto",null,-1)] [InlineData("SKU","Produto",null,1000001)]
    public void RejectsInvalidFields(string sku,string name,string? description,int min)=>Assert.Throws<ArgumentException>(()=>DadosProduto.Criar(sku,name,description,Guid.NewGuid(),null,1,min));
    [Fact] public void RequiresCategoryAndValidOptionalBrand(){Assert.Throws<ArgumentException>(()=>DadosProduto.Criar("SKU","Produto",null,Guid.Empty,null,1,0));Assert.Throws<ArgumentException>(()=>DadosProduto.Criar("SKU","Produto",null,Guid.NewGuid(),Guid.Empty,1,0));}
    public static byte[] Image(SKEncodedImageFormat format=SKEncodedImageFormat.Png,int width=2,int height=2){using var b=new SKBitmap(width,height);b.Erase(SKColors.Blue);using var image=SKImage.FromBitmap(b);using var data=image.Encode(format,90);return data.ToArray();}
    [Theory] [InlineData(SKEncodedImageFormat.Png)] [InlineData(SKEncodedImageFormat.Jpeg)]
    public void ReencodesOnlyDecodedPixels(SKEncodedImageFormat format){var clean=ImagensPrivadas.Validar(Image(format));Assert.Equal(new byte[]{137,80,78,71,13,10,26,10},clean[..8]);using var b=SKBitmap.Decode(clean);Assert.Equal(2,b.Width);}
    [Fact] public void RejectsFakeOversizedCorruptAndLargeDimensions(){Assert.Equal(415,Assert.Throws<CadastroException>(()=>ImagensPrivadas.Validar("<svg/>"u8.ToArray())).Status);Assert.Equal(413,Assert.Throws<CadastroException>(()=>ImagensPrivadas.Validar(new byte[2097153])).Status);Assert.Throws<CadastroException>(()=>ImagensPrivadas.Validar(new byte[]{255,216,255,0,0}));Assert.Throws<CadastroException>(()=>ImagensPrivadas.Validar(Image(width:4097,height:1)));}
    [Theory] [InlineData("../secret.png")] [InlineData("C:\\test.png")] [InlineData("user.png")]
    public void RefusesClientPaths(string path)=>Assert.Throws<CadastroException>(()=>new ImagensPrivadas(Path.GetTempPath()).Caminho(path));
}
