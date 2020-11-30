using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Task3
{
    public class Blob
    {
        public int Id { get; set; }

        public byte[] ImageBytes { get; set; }
    }

    public class Image
    {
        public int Id { get; set; }

        public int Hash { get; set; }

        public string Prediction { get; set; }

        public int RepeatedCallsNumber { get; set; }

        public Blob Blob { get; set; }
    }

    public class ImageRecognitionContext : DbContext
    {

        public DbSet<Image> Images { get; set; }

        public DbSet<Blob> Blobs { get; set; }


        public ImageRecognitionContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ImageRecognitionDB;Trusted_Connection=True;");
        }

        public void Clear()
        {
            lock (this)
            {
                Images.RemoveRange(Images);
                Blobs.RemoveRange(Blobs);
                SaveChanges();
            }
        }
    }
}
