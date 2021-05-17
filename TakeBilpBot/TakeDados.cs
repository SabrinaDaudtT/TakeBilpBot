namespace TakeBilpBot.Model
{
    public class TakeDados
    {
        public string Imagem { get; set; }
        public string Titulo { get; set; }
        public string Descriacao { get; set; }

        public TakeDados()
        {
            Titulo = Descriacao = Imagem = string.Empty;
        }
    }
}
