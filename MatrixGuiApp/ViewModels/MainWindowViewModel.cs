using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace MatrixGuiApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string matrixSize = "3";
        [ObservableProperty] private string inputMatrix = "";
        [ObservableProperty] private string resultText = "";
        [ObservableProperty] private string selectedMethod = "Жордана-Гауса";

        public ObservableCollection<string> AvailableMethods { get; } = new()
        {
            "Жордана-Гауса",
            "Шульца"
        };

        private readonly MatrixService _matrixService = new();

        [RelayCommand]
        private void Solve()
        {
            try
            {
                if (!int.TryParse(MatrixSize, out int size) || size <= 0 || size > 10)
                    throw new Exception("Розмір матриці повинен бути цілим числом від 1 до 10.");

                var matrix = _matrixService.ParseMatrix(InputMatrix, size);

                (double[,] result, string stats) = selectedMethod switch
                {
                    "Жордана-Гауса" => MatrixInverter.InvertByJordanGauss(matrix),
                    "Шульца" => MatrixInverter.InvertBySchulz(matrix),
                    _ => throw new Exception("Невідомий метод")
                };

                ResultText = MatrixFormatter.ToString(result) + "\n" + stats;
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

                InputMatrix = _matrixService.GenerateRandomMatrix(size);
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
                File.WriteAllText("./matrix_result.txt", ResultText);
                ResultText = $"Результат збережено у файл:\n{"./matrix_result.txt"}";
            }
            catch (Exception ex)
            {
                ResultText = $"Помилка збереження: {ex.Message}";
            }
        }
    }

    public class MatrixService
    {
        public string GenerateRandomMatrix(int size)
        {
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
            return sb.ToString();
        }

        public double[,] ParseMatrix(string text, int size)
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
                    if (!double.TryParse(parts[j], out double value))
                        throw new Exception($"Елемент {parts[j]} повинен бути числом.");

                    if ((Math.Abs(value) < MinValue && value != 0) || Math.Abs(value) > MaxValue)
                        throw new Exception($"Елементи повинні бути по модулю в межах від {MinValue} до {MaxValue}, або рівні 0.");

                    matrix[i, j] = value;
                }
            }

            try {
                double determinant = DeterminantCalculator.Determinant(matrix);
                if (Math.Abs(determinant) < 1e-8)
                    throw new Exception("Матриця є сингулярною (визначник дорівнює 0).");
            }
            catch (Exception ex) {
                throw new Exception("Матриця є сингулярною (визначник дорівнює 0).");
            }

            return matrix;
        }
    }

    public static class DeterminantCalculator
    {
        public static double Determinant(double[,] matrix)
        {
        int n = matrix.GetLength(0);

        if (n != matrix.GetLength(1))
            throw new ArgumentException("Матриця повинна бути квадратною.");

        if (n == 1)
            return matrix[0, 0];

        if (n == 2)
            return matrix[0,0] * matrix[1,1] - matrix[0,1] * matrix[1,0];

        double det = 0;

        for (int col = 0; col < n; col++)
        {
            double[,] minor = GetMinor(matrix, 0, col);
            double cofactor = Math.Pow(-1, col) * matrix[0, col];
            det += cofactor * Determinant(minor);
        }

        return det;
    }

    private static double[,] GetMinor(double[,] matrix, int rowToRemove, int colToRemove)
    {
        int n = matrix.GetLength(0);
        double[,] minor = new double[n - 1, n - 1];

        int r = 0;
        for (int i = 0; i < n; i++)
        {
            if (i == rowToRemove) continue;
            int c = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == colToRemove) continue;
                minor[r, c] = matrix[i, j];
                c++;
            }
            r++;
        }

            return minor;
        }
    }


    public static class MatrixFormatter
    {
        public static string ToString(double[,] matrix)
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
    }

    public static class MatrixInverter
    {
        public static (double[,], string) InvertByJordanGauss(double[,] mat)
        {
            int n = mat.GetLength(0);
            double[,] result = new double[n, n];
            double[,] temp = (double[,])mat.Clone();

            for (int i = 0; i < n; i++)
                result[i, i] = 1;

            int iterationCount = 0;

            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                    if (Math.Abs(temp[k, i]) > Math.Abs(temp[maxRow, i]))
                        maxRow = k;

                if (i != maxRow)
                {
                    for (int j = 0; j < n; j++)
                    {
                        (temp[i, j], temp[maxRow, j]) = (temp[maxRow, j], temp[i, j]);
                        (result[i, j], result[maxRow, j]) = (result[maxRow, j], result[i, j]);
                    }
                }

                double diag = temp[i, i];
                if (Math.Abs(diag) < 1e-12)
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
                }
            }

            return (result, $"Жордана-Гауса: {iterationCount} ітерацій.");
        }

        public static (double[,], string) InvertBySchulz(double[,] A)
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
                double[,] AX = Multiply(A, X, ref iterationCount);
                double[,] EminusAX = Subtract(I, AX, ref iterationCount);
                double[,] correction = Multiply(X, EminusAX, ref iterationCount);
                X = Add(X, correction, ref iterationCount);

                double maxResidual = 0;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        maxResidual = Math.Max(maxResidual, Math.Abs(EminusAX[i, j]));

                if (maxResidual < tolerance)
                    break;
            }

            return (X, $"Шульца: {iterationCount} ітерацій.");
        }

        private static double[,] Multiply(double[,] A, double[,] B, ref int count)
        {
            int n = A.GetLength(0);
            double[,] result = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                        count++;
                    }
                }
            return result;
        }

        private static double[,] Subtract(double[,] A, double[,] B, ref int count)
        {
            int n = A.GetLength(0);
            double[,] result = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = A[i, j] - B[i, j];
                    count++;
                }
            return result;
        }

        private static double[,] Add(double[,] A, double[,] B, ref int count)
        {
            int n = A.GetLength(0);
            double[,] result = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = A[i, j] + B[i, j];
                    count++;
                }
            return result;
        }
    }
}