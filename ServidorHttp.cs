using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

class ServidorHttp{
    private TcpListener Controlador { get; set; }
    private int Porta { get; set; }
    private int QtdRequests { get; set; }
    public string HtmlConteudo { get; set; }

    public ServidorHttp(int porta = 8080){
        this.Porta = porta;
        this.CriarHtmlConteudo();
        try{
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e){
            Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}:\n{e.Message}");
        }
    }

    private async Task AguardarRequests(){
        while(true){
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdRequests));
        }
    }

    private void ProcessarRequest(Socket conexao, int numeroRequest){
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if(conexao.Connected){
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            string textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao)
                .Replace((char)0, ' ').Trim();
            if(textoRequisicao.Length > 0){
                Console.WriteLine($"\n{textoRequisicao}\n");

                //Criando metodo pra buscar GET
                string[] linhas = textoRequisicao.Split("\r\n"); //Dividindo o texto em varias lin"ha (Ex.:"GET /index.html HTTP/1.1")
                int iPrimeiroEspaco = linhas[0].IndexOf(' '); //Captura o 1º caracter de espaço(' ')
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');//Captura o ultimo caracter de espaço(' ') nesse caso o 2º
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco); //Extraindo o metodo HTTP que está na lina '0'
                string recursoBuscado = linhas[0].Substring(iPrimeiroEspaco +1, iSegundoEspaco - iPrimeiroEspaco - 1); // Extraindo o nome do recurso buscado.
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1); //Pegando  versão do protocolo Http

                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost = linhas[2].Substring(iPrimeiroEspaco + 1 ); //capturando o nome do host

                byte[] bytesCabecalho = null;
                var bytesConteudo = LerArquivo(recursoBuscado);
                if(bytesConteudo.Length > 0){
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "200", bytesConteudo.Length);
                }else{
                    bytesConteudo = Encoding.UTF8.GetBytes("<h1>ERRO 404 - Arquivo não encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "404", bytesConteudo.Length);
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisição # {numeroRequest}.");
            }
        }   
        Console.WriteLine($"\nRequest {numeroRequest} finalizado.");
    }

    public byte[] GerarCabecalho(string versaoHttp, string tipoMime, string codigoHttp, int qtdeBytes = 0){
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor HTTP 1.0{Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }

    private void CriarHtmlConteudo(){
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title>Página Teste 01</title></head><body>");
        html.Append("<h1>Página Teste 01</h1></body></html>");
        this.HtmlConteudo = html.ToString();
    }

    public byte[] LerArquivo(string recurso){
        //string diretorio = "C:\\dev\\ProjetosCsharp\\ServidorHTTP\\wwww";
        string diretorio = "C:\\dev\\SevidorHTTP\\www";        
        string caminhoArquivo = diretorio + recurso.Replace("/", "\\");
        if(File.Exists(caminhoArquivo)){
           return File.ReadAllBytes(caminhoArquivo); 
        }else
            return new byte[0];
    }
}