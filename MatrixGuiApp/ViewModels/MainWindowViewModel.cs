using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace MatrixGuiApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string matrixSize = "3";

    [ObservableProperty]
    private string inputMatrix = "";

    [ObservableProperty]
    private string resultText = "";

    [ObservableProperty]
    private string selectedMethod = "Жордана-Гауса";

    public ObservableCollection<string> AvailableMethods { get; } = new()
    {
        "Жордана-Гауса",
        "Шульца"
    };

    [RelayCommand]
    private void Solve()
    {
        try
        {
            if (!int.TryParse(MatrixSize, out int size) || size <= 0 || size > 10)
                throw new Exception("Розмір матриці повинен бути цілим числом від 1 до 10.");

            var matrix = ParseMatrix(InputMatrix, size);

            double[,] result;
            string stats;

            (result, stats) = SelectedMethod switch
            {
                "Жордана-Гауса" => InvertByJordanGauss(matrix),
                "Шульца" => InvertBySchulz(matrix),
                _ => throw new Exception("Невідомий метод")
            };

            ResultText = MatrixToString(result) + "\n" + stats;
        }
        catch (Exception ex)
        {
            ResultText = $"Помилка: {ex.Message}";
        }
    }

    [RelayCommand]
    public void GenerateMatrix()
    {
        try
        {
            if (!int.TryParse(MatrixSize, out int size) || size <= 0 || size > 10)
                throw new Exception("Розмір матриці повинен бути цілим числом від 1 до 10.");

            var rand = new Random();
            var sb = new StringBuilder();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                   double value = rand.NextDouble() * 200.0 - 100.0; 
                sb.Append($"{value:F3} ");
                }
                sb.AppendLine();
            }

            InputMatrix = sb.ToString();
        }
        catch (Exception ex)
        {
            ResultText = $"Помилка: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ClearMatrix()
    {
        InputMatrix = string.Empty;
        ResultText = string.Empty;
    }

    [RelayCommand]
    private void SaveResult()
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "matrix_result.txt");
            File.WriteAllText(path, ResultText);
            ResultText += $"\n\nРезультат збережено у файл:\n{path}";
        }
        catch (Exception ex)
        {
            ResultText = $"Помилка збереження: {ex.Message}";
        }
    }

    private double[,] ParseMatrix(string text, int size)
    {
        const double MinValue = 1e-8;
        const double MaxValue = 1e6;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != size)
            throw new Exception("Невірна кількість рядків.");

        double[,] matrix = new double[size, size];
        for (int i = 0; i < size; i++)
        {
            var parts = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != size)
                throw new Exception($"Рядок {i + 1} має не {size} елементів.");
            for (int j = 0; j < size; j++)
            {
                double value = double.Parse(parts[j]);
                if ((Math.Abs(value) < MinValue && value != 0) || Math.Abs(value) > MaxValue)
                 throw new Exception($"Елементи повинні бути по модулю в межах від {MinValue} до {MaxValue}, або рівні 0.");

                matrix[i, j] = value;
            }
        }
        return matrix;
    }

   private string MatrixToString(double[,] matrix)
{
    var sb = new StringBuilder();
    int size = matrix.GetLength(0);

    for (int i = 0; i < size; i++)
    {
        for (int j = 0; j < size; j++)
        {
            double val = matrix[i, j];
            if (Math.Abs(val) < 1e-8)
                sb.Append("0 ");
            else if (Math.Abs(val) < 1e-6)
                sb.Append($"{val:E2} "); 
            else
                sb.Append($"{val:F4} "); 
        }
        sb.AppendLine();
    }

    return sb.ToString();
}

    private int CountIterationsInMultiplication(double[,] A, double[,] B, double[,] result)
    {
        int n = A.GetLength(0);
        int count = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                result[i, j] = 0;
                for (int k = 0; k < n; k++)
                {
                    result[i, j] += A[i, k] * B[k, j];
                    count++;
                }
            }
        return count;
    }

    private int CountIterationsInSubtraction(double[,] A, double[,] B, double[,] result)
    {
        int n = A.GetLength(0);
        int count = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                result[i, j] = A[i, j] - B[i, j];
                count++;
            }
        return count;
    }

    private int CountIterationsInAddition(double[,] A, double[,] B, double[,] result)
    {
        int n = A.GetLength(0);
        int count = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                result[i, j] = A[i, j] + B[i, j];
                count++;
            }
        return count;
    }

    private (double[,], string) InvertByJordanGauss(double[,] mat)
    {
        int n = mat.GetLength(0);
        double[,] result = new double[n, n];
        double[,] temp = (double[,])mat.Clone();

        for (int i = 0; i < n; i++)
            result[i, i] = 1;

        int iterationCount = 0;

        for (int i = 0; i < n; i++)
        {
            // Pivoting
            int maxRow = i;
            for (int k = i + 1; k < n; k++)
            {
                if (Math.Abs(temp[k, i]) > Math.Abs(temp[maxRow, i]))
                    maxRow = k;
            }
            if (i != maxRow)
            {
                for (int j = 0; j < n; j++)
                {
                    (temp[i, j], temp[maxRow, j]) = (temp[maxRow, j], temp[i, j]);
                    (result[i, j], result[maxRow, j]) = (result[maxRow, j], result[i, j]);
                }
            }

            double diag = temp[i, i];
            iterationCount++;
            if (CompareAlmostZero(diag))
                throw new Exception("Неможливо інвертувати: нуль або майже нуль на головній діагоналі.");

            for (int j = 0; j < n; j++)
            {
                temp[i, j] /= diag;
                result[i, j] /= diag;
                iterationCount += 2;
            }

            for (int k = 0; k < n; k++)
            {
                if (k == i) continue;
                double factor = temp[k, i];
                for (int j = 0; j < n; j++)
                {
                    temp[k, j] -= factor * temp[i, j];
                    result[k, j] -= factor * result[i, j];
                    iterationCount += 2;
                }
                iterationCount++;
            }
        }

        string stats = $"Жордана-Гауса: {iterationCount} ітерацій.";
        return (result, stats);
    }

    private (double[,], string) InvertBySchulz(double[,] A)
    {
        int n = A.GetLength(0);
        double[,] X = new double[n, n];
        double[,] I = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            I[i, i] = 1;
            for (int j = 0; j < n; j++)
                X[i, j] = A[j, i];
        }

        double norm = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                norm += A[i, j] * A[i, j];

        norm = 1.0 / norm;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                X[i, j] *= norm;

        int iterationCount = 0;
        double tolerance = 1e-8;

        for (int iter = 0; iter < 25; iter++)
        {
            double[,] AX = new double[n, n];
            iterationCount += CountIterationsInMultiplication(A, X, AX);

            double[,] EminusAX = new double[n, n];
            iterationCount += CountIterationsInSubtraction(I, AX, EminusAX);

            double[,] correction = new double[n, n];
            iterationCount += CountIterationsInMultiplication(X, EminusAX, correction);

            iterationCount += CountIterationsInAddition(X, correction, X);

            double maxResidual = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    maxResidual = Math.Max(maxResidual, Math.Abs(EminusAX[i, j]));

            if (maxResidual < tolerance)
                break;
        }

        string stats = $"Шульца: {iterationCount} ітерацій.";
        return (X, stats);
    }

    private bool CompareAlmostZero(double value, double epsilon = 1e-12)
    {
        return Math.Abs(value) < epsilon;
    }
}
