using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows.Input;

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

    public string[] AvailableMethods => new[] { "Жордана-Гауса", "Шульца" };

    [RelayCommand]
    private void Solve()
    {
        try
        {
            var size = int.Parse(MatrixSize);
            var matrix = ParseMatrix(InputMatrix, size);

            double[,] result = selectedMethod switch
            {
                "Жордана-Гауса" => InvertByJordanGauss(matrix),
                "Шульца" => InvertBySchulz(matrix),
                _ => throw new Exception("Невідомий метод")
            };

            ResultText = MatrixToString(result);
        }
        catch (Exception ex)
        {
            ResultText = $"Помилка: {ex.Message}";
        }
    }

    private double[,] ParseMatrix(string text, int size)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != size)
            throw new Exception("Невірна кількість рядків.");

        double[,] matrix = new double[size, size];
        for (int i = 0; i < size; i++)
        {
            var parts = lines[i].Trim().Split(' ');
            if (parts.Length != size)
                throw new Exception($"Рядок {i + 1} не має {size} елементів.");
            for (int j = 0; j < size; j++)
                matrix[i, j] = double.Parse(parts[j]);
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
                sb.Append($"{matrix[i, j]:F4} ");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private double[,] InvertByJordanGauss(double[,] mat)
    {
        // Проста реалізація Жордана-Гауса
        int n = mat.GetLength(0);
        double[,] result = new double[n, n];
        double[,] temp = (double[,])mat.Clone();

        // Створення одиничної матриці
        for (int i = 0; i < n; i++)
            result[i, i] = 1;

        for (int i = 0; i < n; i++)
        {
            double diag = temp[i, i];
            if (diag == 0)
                throw new Exception("Неможливо інвертувати: нуль на головній діагоналі.");

            for (int j = 0; j < n; j++)
            {
                temp[i, j] /= diag;
                result[i, j] /= diag;
            }

            for (int k = 0; k < n; k++)
            {
                if (k == i) continue;
                double factor = temp[k, i];
                for (int j = 0; j < n; j++)
                {
                    temp[k, j] -= factor * temp[i, j];
                    result[k, j] -= factor * result[i, j];
                }
            }
        }

        return result;
    }

    private double[,] InvertBySchulz(double[,] A)
    {
        // Метод Шульца (наближений)
        int n = A.GetLength(0);
        double[,] X = (double[,])A.Clone(); // початкове наближення
        double[,] I = new double[n, n];
        for (int i = 0; i < n; i++) I[i, i] = 1;

        for (int iter = 0; iter < 10; iter++)
        {
            var AX = Multiply(A, X);
            var EminusAX = Subtract(I, AX);
            var correction = Multiply(X, EminusAX);
            X = Add(X, correction);
        }

        return X;
    }

    private double[,] Multiply(double[,] A, double[,] B)
    {
        int n = A.GetLength(0);
        double[,] result = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                for (int k = 0; k < n; k++)
                    result[i, j] += A[i, k] * B[k, j];
        return result;
    }

    private double[,] Subtract(double[,] A, double[,] B)
    {
        int n = A.GetLength(0);
        double[,] result = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                result[i, j] = A[i, j] - B[i, j];
        return result;
    }

    private double[,] Add(double[,] A, double[,] B)
    {
        int n = A.GetLength(0);
        double[,] result = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                result[i, j] = A[i, j] + B[i, j];
        return result;
    }
}
