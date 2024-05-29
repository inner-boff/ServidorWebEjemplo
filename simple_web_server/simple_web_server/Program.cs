using System; // Espacio de nombres para clases básicas como String, Int32, etc.
using System.IO; // Espacio de nombres para clases que permiten la manipulación de archivos y directorios.
using System.Net; // Espacio de nombres para clases relacionadas con el manejo de solicitudes de red.
using System.Text; // Espacio de nombres para clases que permiten la manipulación de texto.
using System.Threading.Tasks; // Espacio de nombres para clases relacionadas con tareas asincrónicas.
using System.IO.Compression; // Espacio de nombres para clases que permiten la compresión de archivos.

class SimpleWebServer
{
    // Variables estáticas para almacenar el directorio raíz y el puerto de escucha
    private static string rootDirectory;
    private static int port;

    // Método principal que inicia la ejecución del servidor
    static async Task Main(string[] args)
    {
        // Leer el directorio raíz desde el archivo de configuración y asignarlo a rootDirectory
        rootDirectory = File.ReadAllText("archivos_config.txt").Trim();
        // Leer el puerto desde el archivo de configuración y asignarlo a port
        port = int.Parse(File.ReadAllText("puerto_config.txt").Trim());

        // Crear y configurar el HttpListener para escuchar en el puerto especificado
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        Console.WriteLine($"Listening on port {port}. Serving files from {rootDirectory}");

        // Bucle infinito para manejar solicitudes entrantes de forma concurrente
        while (true)
        {
            // Esperar a que llegue una solicitud HTTP
            HttpListenerContext context = await listener.GetContextAsync();
            // Manejar la solicitud en un nuevo hilo para concurrencia
            _ = Task.Run(() => HandleRequest(context));
        }
    }

    // Método para manejar las solicitudes HTTP entrantes
    private static async Task HandleRequest(HttpListenerContext context)
    {
        // Obtener la ruta de la URL solicitada, eliminando la barra inicial
        string urlPath = context.Request.Url.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(urlPath))
        {
            // Si no se especifica un archivo, servir index.html
            urlPath = "index.html";
        }

        // Construir la ruta completa del archivo solicitado
        string filePath = Path.Combine(rootDirectory, urlPath);
        if (!File.Exists(filePath))
        {
            // Si el archivo no existe, servir el archivo de error 404
            filePath = Path.Combine(rootDirectory, "error_404.html");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound; // Establecer el código de estado 404
        }

        // Manejar solicitudes POST
        if (context.Request.HttpMethod == "POST")
        {
            using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                // Leer los datos del cuerpo de la solicitud POST
                string postData = await reader.ReadToEndAsync();
                // Registrar los datos recibidos
                LogData($"POST data: {postData}");
            }
        }

        // Manejar solicitudes GET
        if (context.Request.HttpMethod == "GET")
        {
            // Obtener los parámetros de consulta de la URL
            string query = context.Request.Url.Query;
            if (!string.IsNullOrEmpty(query))
            {
                // Registrar los datos de consulta
                LogData($"Query data: {query}");
            }
        }

        // Registrar información de la solicitud, incluyendo la IP de origen y la URL solicitada
        LogData($"Request from {context.Request.RemoteEndPoint} for {context.Request.Url}");

        // Servir el archivo solicitado con compresión GZIP
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // Establecer el tipo de contenido del archivo
            context.Response.ContentType = GetContentType(filePath);
            // Añadir encabezado para indicar compresión GZIP
            context.Response.AddHeader("Content-Encoding", "gzip");

            // Comprimir y enviar el contenido del archivo
            using (GZipStream gzip = new GZipStream(context.Response.OutputStream, CompressionMode.Compress))
            {
                await fs.CopyToAsync(gzip);
            }
        }

        // Cerrar la respuesta
        context.Response.Close();
    }

    // Método para registrar datos en un archivo de log
    private static void LogData(string data)
    {
        // Crear el nombre del archivo de log basado en la fecha actual
        string logFile = Path.Combine("logs", $"{DateTime.Now:yyyy-MM-dd}.log");
        // Crear el directorio de logs si no existe
        Directory.CreateDirectory("logs");
        // Escribir los datos en el archivo de log
        File.AppendAllText(logFile, $"{DateTime.Now:HH:mm:ss} - {data}\n");
    }

    // Método para obtener el tipo de contenido basado en la extensión del archivo solicitado
    private static string GetContentType(string filePath)
    {
        // Obtener la extensión del archivo en minúsculas
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        // Devolver el tipo de contenido correspondiente a la extensión
        return extension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }
}


/*

Explicación detallada:

Espacios de nombres (using ...): Importan varias bibliotecas necesarias para manipular archivos, manejar solicitudes de red, realizar tareas asincrónicas, y comprimir archivos.

Clase SimpleWebServer: Esta clase contiene el método principal y las funciones para manejar las solicitudes HTTP.

Variables estáticas rootDirectory y port: Almacenan el directorio raíz desde donde se servirán los archivos y el puerto de escucha respectivamente.

-Main: Método principal que inicia el servidor.
	Lee la configuración desde los archivos de texto y asigna los valores del directorio raíz y el puerto.
	Configura el HttpListener para escuchar en el puerto especificado.
	Inicia un bucle infinito para manejar solicitudes entrantes de forma concurrente, creando una nueva tarea para cada solicitud.
-HandleRequest: Método que maneja las solicitudes HTTP entrantes.
	Determina el archivo solicitado, manejando el caso en que el archivo no existe y sirviendo un archivo de error 404.
	Maneja solicitudes POST registrando los datos recibidos.
	Maneja solicitudes GET registrando los parámetros de consulta.
	Registra la información de la solicitud.
	Sirve el archivo solicitado con compresión GZIP.
-LogData: Método que registra datos en un archivo de log.
	Crea el directorio de logs si no existe.
	Escribe los datos en un archivo de log cuyo nombre está basado en la fecha actual.
-GetContentType: Método que devuelve el tipo de contenido basado en la extensión del archivo solicitado.

Este código ahora está completamente comentado, proporcionando una explicación clara de cada parte del código, lo que facilita su comprensión y mantenimiento.


*/