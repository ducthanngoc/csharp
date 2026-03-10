using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bai1
{
    class Student
    {
        public int id 
        {
            get; set; }
        public string name
        {
            get; set;
        }
        public int age
        {
            get; set;
        }
        public double score
        {
            get; set;
        }
    }
    internal class Program
    {
        static List<Student> GetStudentByScore(List<Student> dssv, double score)
        {
            return dssv.Where(sv => sv.score >= score).ToList();
        }
        static List<Student> SortStudentByScore(List<Student> dssv, string type)
        {
            if (type == "des")
            {
                return dssv.OrderByDescending(sv => sv.score).ToList();
            }
            else
            {
                return dssv.OrderBy(sv => sv.score).ToList();
            }
        }
        static List<Student> GetHighestScoreStudent(List<Student> dssv)
        {
            double maxScore = dssv.Max(sv => sv.score);

            return dssv.Where(sv => sv.score == maxScore).ToList();
        }
        static double GetAverageScore(List<Student> dssv)
        {
            return dssv.Average(sv => sv.score);
        }
        static void Main(string[] args)
        {
            List<Student> dssv = new List<Student>();
            int id;
            string name;
            int age;
            double score;
            for (int i = 0; i < 6; i++)
            {
                Console.WriteLine("Nhap thong tin sinh vien thu {0}", i + 1);
                
                Console.Write("Nhap id: ");
                id = int.Parse(Console.ReadLine());
                
                Console.Write("Nhap ten: ");
                name = Console.ReadLine();

                Console.Write("Nhap tuoi: ");
                age = int.Parse(Console.ReadLine());

                Console.Write("Nhap diem: ");
                score = double.Parse(Console.ReadLine());
                dssv.Add(new Student { id = id, name = name, age = age, score = score});
            }
            Console.WriteLine("\nDanh sach sinh vien:");

            foreach (Student sv in dssv)
            {
                Console.WriteLine($"{sv.id} - {sv.name} - {sv.age} - {sv.score}");
            }
            Console.WriteLine("\nDanh sach sinh vien co diem >=7:");
            List<Student> ds1 = GetStudentByScore(dssv, 7);
            foreach (Student sv in ds1)
            {
                Console.WriteLine($"{sv.id} - {sv.name} - {sv.age} - {sv.score}");
            }
            Console.WriteLine("\nDanh sach sinh vien sap xep theo diem giam dan:");
            List<Student> ds2 = SortStudentByScore(dssv, "des");
            foreach (Student sv in ds2)
            {
                Console.WriteLine($"{sv.id} - {sv.name} - {sv.age} - {sv.score}");
            }
            ds2 = GetHighestScoreStudent(dssv);
            Console.WriteLine("\nDanh sach sinh vien co diem cao nhat:");
            foreach (Student sv in ds2)
            {
                Console.WriteLine($"{sv.id} - {sv.name} - {sv.age} - {sv.score}");
            }
            double diemtb = GetAverageScore(dssv);
            Console.WriteLine($"Diem trung binh cua tat ca sinh vien: {diemtb}");
        }
    }
}
