public static class JordanGauss
{
    public static (double[,], int) Invert(double[,] matrix)
    {
        int n = matrix.GetLength(0);
        double[,] augmented = new double[n, 2 * n];
        double[,] result = new double[n, n];
        int ops = 0;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                augmented[i, j] = matrix[i, j];
            augmented[i, n + i] = 1;
        }

        for (int i = 0; i < n; i++)
        {
            double pivot = augmented[i, i];
            if (pivot == 0) throw new Exception("Матриця не є оберненою.");

            for (int j = 0; j < 2 * n; j++) { augmented[i, j] /= pivot; ops++; }

            for (int k = 0; k < n; k++)
            {
                if (k == i) continue;
                double factor = augmented[k, i];
                for (int j = 0; j < 2 * n; j++) { augmented[k, j] -= factor * augmented[i, j]; ops++; }
            }
        }

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                result[i, j] = augmented[i, j + n];

        return (result, ops);
    }
}
