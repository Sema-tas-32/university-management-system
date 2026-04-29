namespace Obs_Proje.Models
{
    public class OgrenciNotu
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string LessonName { get; set; }
        public int ClassLevel { get; set; }
        public double MidtermExam { get; set; }
        public double FinalExam { get; set; }
        public double Average { get; set; }
        public bool IsPassed { get; set; }
        public int Attendance { get; set; } // Devamsızlık için
        
        // --- BU SATIRI EKLE ---
        public string LetterGrade { get; set; } 
    }
}