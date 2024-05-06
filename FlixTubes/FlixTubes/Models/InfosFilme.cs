using Newtonsoft.Json;
using System.IO;

namespace FlixTubes.Models
{
    public class InfosFilme
    {
        public string? Nome { get; set; }
        public string? Sinopse { get; set; }
        public string? IDVideoYoutube { get; set; }
    }

    public static class InfosServicos
    {
        public static void SaveToJsonFile(string directory,string nomeArquivo, object data)
        {
            string filePath = Path.Combine(directory, $"{nomeArquivo}.json");

            // Serializa o objeto para formato JSON
            string json = JsonConvert.SerializeObject(data);

            // Escreve ou sobrescreve o arquivo
            File.WriteAllText(filePath, json);
        }

        public static InfosFilme? ReadFromJsonFile(string directory, string nomeArquivo)
        {
            string filePath = Path.Combine(directory, $"{nomeArquivo}.json");

            // Verifica se o arquivo existe
            if (!File.Exists(filePath))
                return null;

            // Lê todo o conteúdo do arquivo
            string json = File.ReadAllText(filePath);

            // Deserializa o conteúdo do arquivo JSON para o tipo especificado
            InfosFilme? data = JsonConvert.DeserializeObject<InfosFilme>(json);

            return data;
        }
    }
}
