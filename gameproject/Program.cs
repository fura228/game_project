using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// TODO: 
/// DONE
/// реализовать енам и еще пару штук из списка (не все использовалось)
/// DONE 
/// DONE
/// </summary>

// DTO для сохранения игры
public class SavedGame
{
    public string[] Board { get; set; } = new string[8];
    public string Player1Name { get; set; } = "";
    public string Player2Name { get; set; } = "";
    public bool IsPlayer1Human { get; set; }
    public bool IsPlayer2Human { get; set; }
    public char CurrentPlayerSymbol { get; set; }
}

public static class GameSaver
{
    private const string SaveFilePath = "saved_game.json";

    public static void SaveGame(ReversiBoard board, Player p1, Player p2, char currentPlayerSymbol)
    {
        var saved = new SavedGame
        {
            Board = new string[8],
            Player1Name = p1.Name,
            Player2Name = p2.Name,
            IsPlayer1Human = p1 is HumanPlayer,
            IsPlayer2Human = p2 is HumanPlayer,
            CurrentPlayerSymbol = currentPlayerSymbol
        };

        for (int i = 0; i < 8; i++)
        {
            char[] row = new char[8];
            for (int j = 0; j < 8; j++)
            {
                row[j] = board.GetCell(i, j);
            }
            saved.Board[i] = new string(row);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(saved, options);
        File.WriteAllText(SaveFilePath, json);
    }

    public static SavedGame? LoadSavedGame()
    {
        if (!File.Exists(SaveFilePath)) return null;
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            return JsonSerializer.Deserialize<SavedGame>(json);
        }
        catch
        {
            return null;
        }
    }

    public static bool HasSavedGame() => File.Exists(SaveFilePath);

    public static void DeleteSavedGame()
    {
        if (File.Exists(SaveFilePath))
            File.Delete(SaveFilePath);
    }
}

// Базовый класс для игрока
public abstract class Player
{
    public string Name { get; protected set; }
    public char Symbol { get; protected set; }

    protected Player(string name, char symbol)
    {
        Name = name;
        Symbol = symbol;
    }

    // Полиморфизм: каждый игрок по-своему делает ход
    public abstract (int row, int col)? MakeMove(ReversiBoard board);
}

public class AIPlayer : Player
{
    public AIPlayer(string name, char symbol) : base(name, symbol)
    {

    }

    private const int SIZE = 8;
    public const char EMPTY = '.';
    public const char BLACK = 'B';
    public const char WHITE = 'W';

    private List<(int row, int col)> GetValidMoves(ReversiBoard gameBoard, char playerSymbol)
    {
        char opponent = playerSymbol == BLACK ? WHITE : BLACK;
        List<(int row, int col)> movesList = new List<(int, int)>();

        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int row = 0; row < SIZE; row++)
        {
            for (int col = 0; col < SIZE; col++)
            {
                if (gameBoard.GetCell(row, col) != ReversiBoard.EMPTY)
                    continue;

                bool isValid = false;
                for (int d = 0; d < 8; d++)
                {
                    int r = row + dr[d];
                    int c = col + dc[d];
                    bool foundOpponent = false;

                    while (r >= 0 && r < SIZE && c >= 0 && c < SIZE && gameBoard.GetCell(r, c) == opponent)
                    {
                        r += dr[d];
                        c += dc[d];
                        foundOpponent = true;
                    }

                    if (foundOpponent && r >= 0 && r < SIZE && c >= 0 && c < SIZE && gameBoard.GetCell(r, c) == playerSymbol)
                    {
                        isValid = true;
                        break;
                    }
                }

                if (isValid)
                {
                    movesList.Add((row, col));
                }
            }
        }

        return movesList;
    }

    public override (int row, int col)? MakeMove(ReversiBoard board)
    {
        var validMoves = GetValidMoves(board, Symbol);
        Console.WriteLine($"Найдено допустимых ходов: {validMoves.Count}");

        if (validMoves.Count == 0)
            return null;

        Random random = new Random();
        var move = validMoves[random.Next(validMoves.Count)];

        Console.WriteLine($"Бот делает ход: {move.row + 1}, {move.col + 1}");
        Console.WriteLine("Нажмите Enter, чтобы продолжить...");
        Console.ReadKey(intercept: true);
        return (move.row, move.col);
    }
}

// Конкретный класс игрока-человека
public class HumanPlayer : Player
{
    public HumanPlayer(string name, char symbol) : base(name, symbol) { }

    public override (int row, int col)? MakeMove(ReversiBoard board)
    {
        while (true)
        {
            Console.Write($"{Name}, введите строку и столбец (например: 4 5), 'save' для сохранения или exit для выхода: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input == "exit")
            {
                return null;
            }
            if (input == "save")
            {
                return (-2, -2);
            }

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
            {
                Console.WriteLine($"{Name} делает ход: {r}, {c}");
                Console.WriteLine("Нажмите Enter, чтобы продолжить...");
                Console.ReadKey(intercept: true);
                // Преобразуем к внутренним индексам
                return (r - 1, c - 1);
            }
        }
    }
}


// Класс для хранения статистики одного игрока
public class PlayerScore
{
    public string Name { get; set; }
    public int Wins { get; set; }
    public int GamesPlayed { get; set; }

    public PlayerScore(string name, int wins = 0, int gamesPlayed = 0)
    {
        Name = name;
        Wins = wins;
        GamesPlayed = gamesPlayed;
    }


    public double WinRate => GamesPlayed == 0 ? 0 : (double)Wins / GamesPlayed * 100;
}

// Менеджер рейтинга — загружает, сохраняет, обновляет
// Записываем результаты в джейсонку (работа с файлом реализована с помощью ИИ)
public static class ScoreManager
{
    private static readonly string FilePath = "scores.json";
    private static List<PlayerScore> _scores = null;

    // Загружает или создаёт список при первом обращении
    public static List<PlayerScore> Scores
    {
        get
        {
            if (_scores == null)
            {
                LoadScores();
            }
            return _scores;
        }
    }

    public static void LoadScores()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                _scores = JsonSerializer.Deserialize<List<PlayerScore>>(json) ?? new List<PlayerScore>();
                Console.WriteLine($"Загружено {Scores.Count} игроков");
            }
            catch
            {
                _scores = new List<PlayerScore>();
            }
        }
        else
        {
            _scores = new List<PlayerScore>();
        }
    }

    public static void SaveScores()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Scores, options);
            File.WriteAllText(FilePath, json);
        }
        catch { /* игнорируем ошибки записи */ }
    }

    // Найти игрока по имени (регистронезависимо) или создать нового
    public static PlayerScore GetOrCreate(string name)
    {
        var player = Scores.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (player == null)
        {
            player = new PlayerScore(name);
            Scores.Add(player);
        }
        return player;
    }

    // Добавить победу игроку (и сохранить файл)
    public static void RecordWin(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        var player = GetOrCreate(playerName);
        player.Wins++;
        SaveScores();
    }

    // Вывести топ-N игроков (снимаем грядущую проблему в несколько сотен тысяч игроков)
    public static void ShowLeaderboard(int topN = 10)
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine(" ТАБЛИЦА ЛИДЕРОВ ");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine("№  Имя".PadRight(20) + " Победы  Игры   %");
        Console.WriteLine(new string('-', 50));

        var ranked = Scores
        .Where(p => p.GamesPlayed > 0)  // Только те, кто играл
        .OrderByDescending(p => p.WinRate)  // Сначала по % побед
        .ThenByDescending(p => p.Wins)      // Потом по числу побед
        .ThenBy(p => p.Name)                // Потом по имени
        .Take(topN)
        .ToList();

        if (ranked.Count == 0)
        {
            Console.WriteLine("— Пока нет победителей —");
        }
        else
        {
            for (int i = 0; i < ranked.Count; i++)
            {
                var p = ranked[i];
                double rate = Math.Round(p.WinRate, 1);
                Console.WriteLine($"{(i + 1),2}. {p.Name.PadRight(16)} {p.Wins,5}  {p.GamesPlayed,5}  {rate,5:F1}%");
            }
        }
        Console.WriteLine(new string('=', 30) + "\n");
    }
    public static void RecordGame(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        var player = GetOrCreate(playerName);
        player.GamesPlayed++;
        SaveScores();
    }
}


// Логика доски
public class ReversiBoard
{
    private const int SIZE = 8;
    private char[,] board = new char[SIZE, SIZE];
    public const char EMPTY = '.';
    public const char BLACK = 'B';
    public const char WHITE = 'W';

    public ReversiBoard()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        // Очистка
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                board[i, j] = EMPTY;
            }
        }

        // Начальная расстановка
        board[3, 3] = WHITE;
        board[4, 4] = WHITE;
        board[3, 4] = BLACK;
        board[4, 3] = BLACK;
    }

    public void LoadFromStrings(string[] rows)
    {
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                board[i, j] = rows[i][j];
            }
        }
    }

    public char GetCell(int row, int col) => board[row, col];

    public bool IsValidMove(int row, int col, char playerSymbol)
    {
        if (row < 0 || row >= SIZE || col < 0 || col >= SIZE) return false;
        if (board[row, col] != EMPTY) return false;

        char opponent = playerSymbol == BLACK ? WHITE : BLACK;

        // 8 направлений
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int d = 0; d < 8; d++)
        {
            int r = row + dr[d];
            int c = col + dc[d];
            bool foundOpponent = false;

            while (r >= 0 && r < SIZE && c >= 0 && c < SIZE && board[r, c] == opponent)
            {
                r += dr[d];
                c += dc[d];
                foundOpponent = true;
            }

            if (foundOpponent && r >= 0 && r < SIZE && c >= 0 && c < SIZE && board[r, c] == playerSymbol)
            {
                return true;
            }
        }
        return false;
    }

    public void MakeMove(int row, int col, char playerSymbol)
    {
        board[row, col] = playerSymbol;
        FlipDiscs(row, col, playerSymbol);
    }

    private void FlipDiscs(int row, int col, char playerSymbol)
    {
        char opponent = playerSymbol == BLACK ? WHITE : BLACK;
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int d = 0; d < 8; d++)
        {
            int r = row + dr[d];
            int c = col + dc[d];
            var toFlip = new (int, int)[SIZE];
            int flipCount = 0;

            while (r >= 0 && r < SIZE && c >= 0 && c < SIZE && board[r, c] == opponent)
            {
                toFlip[flipCount++] = (r, c);
                r += dr[d];
                c += dc[d];
            }

            if (flipCount > 0 && r >= 0 && r < SIZE && c >= 0 && c < SIZE && board[r, c] == playerSymbol)
            {
                for (int i = 0; i < flipCount; i++)
                {
                    var (fr, fc) = toFlip[i];
                    board[fr, fc] = playerSymbol;
                }
            }
        }
    }

    public bool HasValidMoves(char playerSymbol)
    {
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
                if (IsValidMove(i, j, playerSymbol))
                    return true;
        return false;
    }

    public (int black, int white) GetScores()
    {
        int black = 0, white = 0;
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
            {
                black += board[i, j] == BLACK ? 1 : 0;
                white += board[i, j] == WHITE ? 1 : 0;
            }
        return (black, white);
    }

    public void Display()
    {

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  1 2 3 4 5 6 7 8");
        for (int i = 0; i < SIZE; i++)
        {
            Console.Write($"{i + 1} ");
            for (int j = 0; j < SIZE; j++)
            {
                char cell = board[i, j];
                switch (cell)
                {
                    case BLACK:
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.Write("●");
                        break;
                    case WHITE:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("●");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write("·");
                        break;
                }
                Console.ResetColor();
                Console.Write(" ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}

// Основной класс игры
public class ReversiGame
{
    private ReversiBoard board;
    private Player player1;
    private Player player2;
    private bool gameRunning = true;
    private Player? _abandonedBy;

    public ReversiGame(Player p1, Player p2)
    {
        player1 = p1;
        player2 = p2;
        board = new ReversiBoard();
    }

    public void Start()
    {
        char currentPlayerSymbol = ReversiBoard.BLACK;
        Player currentPlayer = player1;
        RunGameLoop(currentPlayer, currentPlayerSymbol);
    }

    public void ResumeFrom(SavedGame saved)
    {
        var loadedBoard = new ReversiBoard();
        loadedBoard.LoadFromStrings(saved.Board);
        board = loadedBoard;

        char currentPlayerSymbol = saved.CurrentPlayerSymbol;
        Player currentPlayer = currentPlayerSymbol == ReversiBoard.BLACK ? player1 : player2;

        RunGameLoop(currentPlayer, currentPlayerSymbol);
    }

    private void RunGameLoop(Player currentPlayer, char currentPlayerSymbol)
    {
        while (gameRunning)
        {
            Console.Clear();
            board.Display();

            var scores = board.GetScores();
            Console.WriteLine($"{player1.Name} (●): {scores.black} | {player2.Name} (●): {scores.white}\n");

            if (!board.HasValidMoves(currentPlayerSymbol))
            {
                if (!board.HasValidMoves(currentPlayerSymbol == ReversiBoard.BLACK ? ReversiBoard.WHITE : ReversiBoard.BLACK))
                {
                    EndGame();
                    return;
                }
                Console.WriteLine($"{currentPlayer.Name} пропускает ход (нет доступных ходов).");
                System.Threading.Thread.Sleep(1500);
                // Смена игрока
                currentPlayer = currentPlayer == player1 ? player2 : player1;
                currentPlayerSymbol = currentPlayerSymbol == ReversiBoard.BLACK ? ReversiBoard.WHITE : ReversiBoard.BLACK;
                continue;
            }

            var move = currentPlayer.MakeMove(board);

            if (move == null)
            {
                Abandoned(currentPlayer);
                EndGame();
                return;
            }

            // Обработка сохранения
            if (move.Value.row == -2 && move.Value.col == -2)
            {
                GameSaver.SaveGame(board, player1, player2, currentPlayerSymbol);
                gameRunning = false;
                return;
            }

            int row = move.Value.row;
            int col = move.Value.col;

            if (board.IsValidMove(row, col, currentPlayerSymbol))
            {
                board.MakeMove(row, col, currentPlayerSymbol);
            }
            else
            {
                Console.WriteLine("Недопустимый ход! Попробуйте снова.");
                System.Threading.Thread.Sleep(1500);
                continue;
            }

            // Смена игрока
            currentPlayer = currentPlayer == player1 ? player2 : player1;
            currentPlayerSymbol = currentPlayerSymbol == ReversiBoard.BLACK ? ReversiBoard.WHITE : ReversiBoard.BLACK;
        }
    }

    private void Abandoned(Player playerGaveUp)
    {
        _abandonedBy = playerGaveUp;
    }

    private void EndGame()
    {
        var scores = board.GetScores();
        Console.Clear();
        board.Display();
        Console.WriteLine("Игра завершена!\n");

        Console.WriteLine($"{player1.Name}: {scores.black}");
        Console.WriteLine($"{player2.Name}: {scores.white}");

        string? winnerName = null;

        if (_abandonedBy != null)
        {
            Console.WriteLine($"\n{_abandonedBy.Name} сдался.");
            winnerName = _abandonedBy == player1 ? player2.Name : player1.Name;
        }
        else if (scores.black > scores.white)
        {
            winnerName = player1.Name;
        }
        else if (scores.white > scores.black)
        {
            winnerName = player2.Name;
        }
        else
        {
            Console.WriteLine("\nНичья!");
        }

        ScoreManager.RecordGame(player1.Name);
        ScoreManager.RecordGame(player2.Name);

        // Записываем победу (если есть победитель)
        if (winnerName != null)
        {
            Console.WriteLine($"\nПобедитель: {winnerName}!");
            ScoreManager.RecordWin(winnerName);
        }

        GameSaver.DeleteSavedGame();

        Console.WriteLine("\nНажмите любую клавишу для возврата в меню...");
        Console.ReadKey();
    }
}

// Главное меню и запуск
public class Program
{
    enum Choices { twoPlayers = 1, playAI, ladder, rules, continueGame, exit }
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Меню Реверси ===");
            Console.WriteLine("1. Ввести имена игроков и начать игру");
            Console.WriteLine("2. Игра с ботом");
            Console.WriteLine("3. Таблица лидеров");
            Console.WriteLine("4. Правила");
            if (GameSaver.HasSavedGame())
            {
                Console.WriteLine("5. Продолжить игру");
                Console.WriteLine("6. Выйти из игры");
            }    
                
            else
                Console.WriteLine("5. Выйти из игры");
            
            Console.Write("Выберите действие: ");

            string? input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int choiceNum) || !Enum.IsDefined(typeof(Choices), choiceNum))
            {
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                System.Threading.Thread.Sleep(1000);
                continue;
            }
            Choices choice = (Choices)choiceNum;

            switch (choice)
            {
                case Choices.twoPlayers:
                    Console.Write("Имя первого игрока (● Чёрные): ");
                    string p1Name = Console.ReadLine() ?? "Игрок 1";

                    Console.Write("Имя второго игрока (● Белые): ");
                    string p2Name = Console.ReadLine() ?? "Игрок 2";

                    var player1 = new HumanPlayer(p1Name, ReversiBoard.BLACK);
                    var player2 = new HumanPlayer(p2Name, ReversiBoard.WHITE);

                    var game = new ReversiGame(player1, player2);
                    game.Start();
                    break;

                case Choices.playAI:
                    Console.Write("Имя игрока: ");
                    string p1name = Console.ReadLine() ?? "Игрок";
                    var pLayer1 = new HumanPlayer(p1name, ReversiBoard.BLACK);
                    var playerBot = new AIPlayer("Bot", ReversiBoard.WHITE);

                    var game1 = new ReversiGame(pLayer1, playerBot);
                    game1.Start();
                    break;

                case Choices.ladder:
                    ScoreManager.ShowLeaderboard(15);
                    Console.WriteLine("Нажмите любую клавишу для возврата...");
                    Console.ReadKey();
                    break;
                case Choices.rules:
                    Console.WriteLine("В игре используется квадратная доска размером 8 × 8 клеток (все клетки могут быть одного цвета) и 64 специальные фишки, окрашенные с разных сторон в контрастные цвета, например, в белый и чёрный. Клетки доски нумеруются от верхнего левого угла: вертикали — латинскими буквами, горизонтали — цифрами (по сути дела, можно использовать шахматную доску). Один из игроков играет белыми, другой — чёрными. Делая ход, игрок ставит фишку на клетку доски «своим» цветом вверх.\r\n\r\nВ начале игры в центр доски выставляются 4 фишки: чёрные на d5 и e4, белые на d4 и e5.\r\n\r\nПервый ход делают чёрные. Далее игроки ходят по очереди.\r\nДелая ход, игрок должен поставить свою фишку на одну из клеток доски таким образом, чтобы между этой поставленной фишкой и одной из имеющихся уже на доске фишек его цвета находился непрерывный ряд фишек соперника, горизонтальный, вертикальный или диагональный (другими словами, чтобы непрерывный ряд фишек соперника оказался «закрыт» фишками игрока с двух сторон). Все фишки соперника, входящие в «закрытый» на этом ходе ряд, переворачиваются на другую сторону (меняют цвет) и переходят к ходившему игроку.\r\nЕсли в результате одного хода «закрывается» одновременно более одного ряда фишек противника, то переворачиваются все фишки, оказавшиеся на тех «закрытых» рядах, которые идут от поставленной фишки.\r\nИгрок вправе выбирать любой из возможных для него ходов. Если игрок имеет возможные ходы, он не может отказаться от хода. Если игрок не имеет допустимых ходов, то ход передаётся сопернику.\r\nИгра прекращается, когда на доску выставлены все фишки или когда ни один из игроков не может сделать хода. По окончании игры проводится подсчёт фишек каждого цвета, и игрок, чьих фишек на доске выставлено больше, объявляется победителем. В случае равенства количества фишек засчитывается ничья.");
                    Console.ReadKey();
                    break;

                case Choices.continueGame:
                    var saved = GameSaver.LoadSavedGame();
                    if (saved == null)
                    {
                        Console.WriteLine("Нет сохранённой игры!");
                        System.Threading.Thread.Sleep(1500);
                        break;
                    }

                    Player p1 = saved.IsPlayer1Human
                        ? new HumanPlayer(saved.Player1Name, ReversiBoard.BLACK)
                        : new AIPlayer(saved.Player1Name, ReversiBoard.BLACK);

                    Player p2 = saved.IsPlayer2Human
                        ? new HumanPlayer(saved.Player2Name, ReversiBoard.WHITE)
                        : new AIPlayer(saved.Player2Name, ReversiBoard.WHITE);

                    var resumedGame = new ReversiGame(p1, p2);
                    resumedGame.ResumeFrom(saved);
                    break;

                case Choices.exit:
                    Console.WriteLine("Выход из игры...");
                    return;
            }
        }
    }
}

