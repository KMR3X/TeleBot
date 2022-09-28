using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace telebot01
{
    class Program
    {
        static void Main()
        {
            var teleBot = new TelegramBotClient("***BOT_TOKEN_HERE***");

            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Хранилище");

            teleBot.StartReceiving(Update, ErrorHandler);


            Console.ReadLine();
        }


        /// <summary>
        /// <c>Метод обработки обновлений</c>
        /// </summary>
        /// <param name="teleBot"></param>
        /// <param name="update"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        async static Task Update(ITelegramBotClient teleBot, Update update, CancellationToken cts)
        {
            string storagePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Хранилище\"}";

            var message = update.Message;
            var chatId = message.Chat.Id;
            var me = await teleBot.GetMeAsync();

            if (message != null)
            {
                switch (message.Type)
                {
                    case MessageType.Text:

                        string time = DateTime.Now.ToString("H:mm"); 
                        Console.WriteLine($@"{time}: Сообщение от {message.Chat.FirstName}: {message.Text}");

                        //Проверка на ввод команды /start
                        if (message.Text.ToLower().Contains("/start") == true)
                        {
                            await teleBot.SendTextMessageAsync(chatId, $"Привет! Я {me.FirstName}. Доступные команды: " +
                                "\n/view - просмотреть файлы в хранилище \n/download имя_файла - скачать файл.");
                        }

                        //Проверка на ввод команды на просмотр файлов в хранилище
                        else if (message.Text.ToLower().Contains("/view") == true)
                        {
                            List<string> files = new List<string>();

                            foreach (string s in Directory.GetFiles(storagePath, "*"))
                            {
                                files.Add(Path.GetFileName(s));
                            }
                            if (files.Count != 0)
                            {
                                //Console.WriteLine(string.Join(" \n", files));
                                Message viewMessage = await teleBot.SendTextMessageAsync(chatId, $"{string.Join(" \n", files)}");
                            }
                            else
                            {
                                Console.WriteLine("Нет файлов.");
                                Message viewMessageNull = await teleBot.SendTextMessageAsync(chatId, $"Пусто.");
                            }
                        }

                        //Проверка на ввод команды на скачивание
                        else if (message.Text.ToLower().Contains("/download") == true)
                        {
                            string msgText = message.Text;
                            string fileNameSplit = msgText.Remove(0, msgText.IndexOf(' ') + 1);

                            List<string> files = new List<string>();

                            foreach (string s in Directory.GetFiles(storagePath, "*"))
                            {
                                files.Add(Path.GetFileName(s));
                            }
                            if (files.Count != 0)
                            {
                                foreach (string f in files)
                                {
                                    if (fileNameSplit == f)
                                    {
                                        await using Stream stream = System.IO.File.OpenRead(storagePath + $@"\{f}");

                                        if (System.IO.File.Exists(storagePath + $@"\{f}"))
                                        {
                                            string ext = Path.GetExtension(storagePath + $@"\{f}").ToLower();
                                            Console.WriteLine(ext);
                                            Upload(ext, storagePath, f, teleBot, chatId);
                                        }
                                        stream.Flush();
                                        stream.Close();
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Нет файлов.");
                                Message viewMessageNull = await teleBot.SendTextMessageAsync(chatId, $"Пусто.");
                            }
                        }

                        else
                        {
                            await teleBot.SendTextMessageAsync(chatId, $"Эхо: {message.Text}");
                        }
                        break;


                    case MessageType.Document:
                        Console.WriteLine($"{message.Document.FileId}\n{message.Document.FileName}\n{message.Document.FileSize}");
                        Download(storagePath, message.Document.FileId, message.Document.FileName, teleBot);
                        break;

                    case MessageType.Photo:
                        Console.WriteLine($"{message.Photo.Last().FileId}\n{message.Photo.Last()}");
                        Download(storagePath, message.Photo.Last().FileId, message.Photo.Last().FileId + ".png", teleBot);
                        break;

                    case MessageType.Audio:
                        Console.WriteLine($"{message.Audio.FileId}\n{message.Audio.FileName}\n{message.Audio.FileSize}");
                        Download(storagePath, message.Audio.FileId, message.Audio.FileName, teleBot);
                        break;

                    case MessageType.Video:
                        Console.WriteLine($"{message.Video.FileId}\n{message.Video.FileName}\n{message.Video.FileSize}");
                        Download(storagePath, message.Video.FileId, message.Video.FileName, teleBot);
                        break;

                    case MessageType.Voice:
                        Console.WriteLine($"{message.Voice.FileId}\n{message.Voice.FileSize}");
                        Download(storagePath, message.Voice.FileId, message.Voice.FileId + ".mp3", teleBot);
                        break;

                    default:
                        Console.WriteLine("Неопознанный тип сообщения.");
                        break;
                }
            }
        }


        /// <summary>
        /// <c>Обработчик исключений</c>
        /// </summary>
        /// <param name="teleBot"></param>
        /// <param name="exception"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private static Task ErrorHandler(ITelegramBotClient teleBot, Exception exception, CancellationToken cts)
        {
            var ErrorMessage = exception switch
            {
                Telegram.Bot.Exceptions.ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }


        /// <summary>
        /// <c>Метод для загрузки отправленного пользователем файла на компьютер</c>
        /// </summary>
        /// <param name="storagePath"></param>
        /// <param name="fileId"></param>
        /// <param name="name"></param>
        /// <param name="teleBot"></param>
        static async void Download(string storagePath, string fileId, string name, ITelegramBotClient teleBot)
        {

            var file = await teleBot.GetFileAsync(fileId);
            FileStream fs = new FileStream(storagePath + "_" + name, FileMode.Create);
            await teleBot.DownloadFileAsync(file.FilePath, fs);
            fs.Close();

            fs.Dispose();
            Console.WriteLine("Загружено успешно...\n");
        }


        /// <summary>
        /// <c>Метод для отправки выбранного файла в чат</c>
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="storagePath"></param>
        /// <param name="fileName"></param>
        /// <param name="teleBot"></param>
        /// <param name="chatId"></param>
        static async void Upload(string fileType, string storagePath, string fileName, ITelegramBotClient teleBot, long chatId)
        {
            await using Stream stream = System.IO.File.OpenRead($@"{storagePath}\{fileName}");

            switch (fileType)
            {
                case ".img":
                case ".png":
                case ".jpg":
                case ".jpeg":
                    Message photoMessage = await teleBot.SendPhotoAsync(chatId, new InputOnlineFile(stream, fileName));
                    break;
                case ".mov":
                    Message videoMessage = await teleBot.SendVideoAsync(chatId, new InputOnlineFile(stream, fileName));
                    break;
                case ".mp3":
                    Message audioMessage = await teleBot.SendAudioAsync(chatId, new InputOnlineFile(stream, fileName));
                    break;
                default:
                    Message docMessage = await teleBot.SendDocumentAsync(chatId, new InputOnlineFile(stream, fileName));
                    break;

            }

            stream.Flush();
            stream.Close();
        }

    }
}