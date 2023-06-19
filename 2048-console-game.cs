namespace KTY.Game2048
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class Program
    {
        private static void Main(string[] args)
        {
            Oyun game = new Oyun();
            game.Calistir();
        }
    }

    internal class Oyun
    {
        public ulong skor { get; private set; } // ulong imzasız bir tür değişken olduğundan, negatif bir sayıyı temsil edemez.
        public ulong[,] oyunTahtasi { get; private set; } // board adında iki boyutlu bir dizi oluşturduk

        private readonly int nSatir;
        private readonly int nSutun;
        private readonly Random rastgele = new Random();

        public Oyun()
        {
            this.oyunTahtasi = new ulong[4, 4];
            this.nSatir = this.oyunTahtasi.GetLength(0);
            this.nSutun = this.oyunTahtasi.GetLength(1);
            this.skor = 0;
        }

        public void Calistir()
        {
            bool hasUpdated = true;
            do
            {
                if (hasUpdated)
                {
                    YeniDegerYerlestir();
                }

                Goruntule();

                if (IsDead())
                {
                    using (new ColorOutput(ConsoleColor.Red))
                    {
                        Console.WriteLine("KYABETTİN!!!");
                        break;
                    }
                }

                Console.WriteLine("Sayıları hareket ettirmek için ok tuşlarını kullanın. Çıkmak için Ctrl-C tuşlarına basın.");
                ConsoleKeyInfo input = Console.ReadKey(true); // KULLANICI GİRİŞ YAPANA KADAR BLOKLA
                Console.WriteLine(input.Key.ToString());

                switch (input.Key)
                {
                    case ConsoleKey.UpArrow:
                        hasUpdated = Update(Direction.Up);
                        break;

                    case ConsoleKey.DownArrow:
                        hasUpdated = Update(Direction.Down);
                        break;

                    case ConsoleKey.LeftArrow:
                        hasUpdated = Update(Direction.Left);
                        break;

                    case ConsoleKey.RightArrow:
                        hasUpdated = Update(Direction.Right);
                        break;

                    default:
                        hasUpdated = false;
                        break;
                }
            }
            while (true); // döngüden çıkmak için CTRL-C kullanın

            Console.WriteLine("Çıkmak için herhangi bir tuşa basın...");
            Console.Read();
        }

        private static ConsoleColor GetNumberColor(ulong num)
        {
            switch (num)
            {
                case 0:
                    return ConsoleColor.DarkGray;

                case 2:
                    return ConsoleColor.Green;

                case 4:
                    return ConsoleColor.Magenta;

                case 8:
                    return ConsoleColor.Red;

                case 16:
                    return ConsoleColor.Cyan;

                case 32:
                    return ConsoleColor.Yellow;

                case 64:
                    return ConsoleColor.DarkYellow;

                case 128:
                    return ConsoleColor.DarkCyan;

                case 256:
                    return ConsoleColor.Cyan;

                case 512:
                    return ConsoleColor.DarkMagenta;

                case 1024:
                    return ConsoleColor.Magenta;

                default:
                    return ConsoleColor.Red;
            }
        }

        private static bool Update(ulong[,] board, Direction direction, out ulong score)
        {
            int nRows = board.GetLength(0);
            int nCols = board.GetLength(1);

            score = 0;
            bool hasUpdated = false;

            // Bu noktada ölmemelisin. Güncellemenin sonunda ölü olup olmadığınızı her zaman kontrol ederiz()

            // Satır boyunca mı yoksa sütun boyunca mı bırakılsın? true: satır boyunca iç işlem; false: iç kısmı sütun boyunca işle
            bool isAlongRow = direction == Direction.Left || direction == Direction.Right;

            // İç boyutu artan dizin düzeninde değerleri işlemeli miyiz?
            bool isIncreasing = direction == Direction.Left || direction == Direction.Up;

            int outterCount = isAlongRow ? nRows : nCols;
            int innerCount = isAlongRow ? nCols : nRows;
            int innerStart = isIncreasing ? 0 : innerCount - 1;
            int innerEnd = isIncreasing ? innerCount - 1 : 0;

            Func<int, int> drop = isIncreasing
                ? new Func<int, int>(innerIndex => innerIndex - 1)
                : new Func<int, int>(innerIndex => innerIndex + 1);

            Func<int, int> reverseDrop = isIncreasing
                ? new Func<int, int>(innerIndex => innerIndex + 1)
                : new Func<int, int>(innerIndex => innerIndex - 1);

            Func<ulong[,], int, int, ulong> getValue = isAlongRow
                ? new Func<ulong[,], int, int, ulong>((x, i, j) => x[i, j])
                : new Func<ulong[,], int, int, ulong>((x, i, j) => x[j, i]);

            Action<ulong[,], int, int, ulong> setValue = isAlongRow
                ? new Action<ulong[,], int, int, ulong>((x, i, j, v) => x[i, j] = v)
                : new Action<ulong[,], int, int, ulong>((x, i, j, v) => x[j, i] = v);

            Func<int, bool> innerCondition = index => Math.Min(innerStart, innerEnd) <= index && index <= Math.Max(innerStart, innerEnd);

            for (int i = 0; i < outterCount; i++)
            {
                for (int j = innerStart; innerCondition(j); j = reverseDrop(j))
                {
                    if (getValue(board, i, j) == 0)
                    {
                        continue;
                    }

                    int newJ = j;
                    do
                    {
                        newJ = drop(newJ);
                    }
                    // Sınıra ulaşmadığımız ve yeni konum işgal edilmediği sürece incelemeye devam edin
                    while (innerCondition(newJ) && getValue(board, i, newJ) == 0);

                    if (innerCondition(newJ) && getValue(board, i, newJ) == getValue(board, i, j))
                    {
                        // oyun alanı sınırına ulaşmadık (bir düğüme ulaştık) VE daha önce birleştirme yapılmadı VE düğümlerin değerleri aynıysa
                        // toplayalım
                        ulong newValue = getValue(board, i, newJ) * 2;
                        setValue(board, i, newJ, newValue);
                        setValue(board, i, j, 0);

                        hasUpdated = true;
                        score += newValue;
                    }
                    else
                    {
                        // Sınıra ulaşıldı VEYA...
                        // farklı değere sahip bir düğüme çarptık VEYA...
                        // aynı değere sahip bir düğüme ulaştık ANCAK önceki bir birleştirme gerçekleşmiş
                        //
                        // Basitçe istifleyin
                        newJ = reverseDrop(newJ); // geçerli konumuna geri dönün
                        if (newJ != j)
                        {
                            // bir güncelleme var
                            hasUpdated = true;
                        }

                        ulong value = getValue(board, i, j);
                        setValue(board, i, j, 0);
                        setValue(board, i, newJ, value);
                    }
                }
            }

            return hasUpdated;
        }

        private bool Update(Direction dir)
        {
            ulong score;
            bool isUpdated = Oyun.Update(this.oyunTahtasi, dir, out score);
            this.skor += score;
            return isUpdated;
        }

        private bool IsDead()
        {
            ulong score;
            foreach (Direction dir in new Direction[] { Direction.Down, Direction.Up, Direction.Left, Direction.Right })
            {
                ulong[,] clone = (ulong[,])oyunTahtasi.Clone();
                if (Oyun.Update(clone, dir, out score))
                {
                    return false;
                }
            }

            // her yönü denedi. hiçbiri işe yaramadı.
            return true;
        }

        // Oyun alanını görüntüleyen fonksiyon
        private void Goruntule()
        {
            Console.Clear();
            Console.WriteLine();
            for (int i = 0; i < nSatir; i++)
            {
                for (int j = 0; j < nSutun; j++)
                {
                    using (new ColorOutput(Oyun.GetNumberColor(oyunTahtasi[i, j])))
                    {
                        Console.Write(string.Format("{0,6}", oyunTahtasi[i, j]));
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("Skor: {0}", this.skor);
            Console.WriteLine();
        }

        private void YeniDegerYerlestir()
        {
            // Tüm boş yuvaları bul
            List<Tuple<int, int>> emptySlots = new List<Tuple<int, int>>();
            for (int iRow = 0; iRow < nSatir; iRow++)
            {
                for (int iCol = 0; iCol < nSutun; iCol++)
                {
                    if (oyunTahtasi[iRow, iCol] == 0)
                    {
                        emptySlots.Add(new Tuple<int, int>(iRow, iCol));
                    }
                }
            }

            // En az 1 boş slotumuz olmalı. Kullanıcının ölmediğini bildiğimiz için
            int iSlot = rastgele.Next(0, emptySlots.Count); // rastgele boş bir yuva seçin
            ulong value = rastgele.Next(0, 100) < 95 ? (ulong)2 : (ulong)4; // rastgele 2 (%95 şansla) veya 4 (geri kalan şansla) seçin
            oyunTahtasi[emptySlots[iSlot].Item1, emptySlots[iSlot].Item2] = value;
        }

        #region Utility Classes

        private enum Direction
        {
            Up,
            Down,
            Right,
            Left,
        }

        private class ColorOutput : IDisposable
        {
            public ColorOutput(ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black)
            {
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }

            public void Dispose()
            {
                Console.ResetColor();
            }
        }

        #endregion Utility Classes
    }
}
