using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Obs_Proje.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http; 

namespace Obs_Proje.Controllers
{
    public class HomeController : Controller
    {
        // SQL Bağlantı Adresin
        string connectionString = "Server=SEMA\\SQLEXPRESS01;Database=OkulSistemi;Integrated Security=True;TrustServerCertificate=True;";

        public IActionResult Index()
        {
            return View();
        }

        // --- GİRİŞ YAPMA (GÜNCELLENDİ) ---
        [HttpPost]
        public IActionResult GirisYap(string kullaniciAdi, string sifre)
        {
            try
            {
                // 1. Kullanıcının girdiği verilerdeki boşlukları temizle
                if (kullaniciAdi != null) kullaniciAdi = kullaniciAdi.Trim();
                if (sifre != null) sifre = sifre.Trim();

                using (SqlConnection baglanti = new SqlConnection(connectionString))
                {
                    baglanti.Open();
                    
                    // 2. Sadece Rolü değil, Kullanıcı Adını da (Doğru yazılışıyla) çekiyoruz
                    string sql = "SELECT Username, Role FROM Users WHERE Username = @u AND Password = @p";
                    SqlCommand komut = new SqlCommand(sql, baglanti);
                    komut.Parameters.AddWithValue("@u", kullaniciAdi);
                    komut.Parameters.AddWithValue("@p", sifre);
                    
                    using (SqlDataReader oku = komut.ExecuteReader())
                    {
                        if (oku.Read()) // Eğer kayıt bulunduysa
                        {
                            // Veritabanındaki GERÇEK ismini alıyoruz (Büyük/küçük harf doğrusu)
                            string dbKullaniciAdi = oku["Username"].ToString();
                            string rol = oku["Role"].ToString();

                            // EĞER HOCAYSA
                            if (rol == "Ogretmen")
                            {
                                string yetkiliDers = "";

                                // Artık karşılaştırmayı Veritabanından gelen doğru isme göre yapıyoruz
                                if (dbKullaniciAdi == "Erhan Akbal") yetkiliDers = "Bilgisayar Sistemleri";
                                else if (dbKullaniciAdi == "Mehmet Veysel Gün") yetkiliDers = "VTYS";
                                else if (dbKullaniciAdi == "İlhan Kılınçer") yetkiliDers = "Bilgisayar Ağları";
                                else if (dbKullaniciAdi == "Mustafa Kaya") yetkiliDers = "Veri Yapıları";
                                else yetkiliDers = "Genel"; 

                                // Session'a veritabanındaki doğru ismi kaydediyoruz
                                HttpContext.Session.SetString("KullaniciAdi", dbKullaniciAdi);
                                HttpContext.Session.SetString("YetkiliDers", yetkiliDers);
                                HttpContext.Session.SetString("Rol", "Ogretmen");

                                return RedirectToAction("OgretmenPaneli");
                            }
                            // EĞER ÖĞRENCİYSE
                            else
                            {
                                HttpContext.Session.SetString("KullaniciAdi", dbKullaniciAdi);
                                HttpContext.Session.SetString("Rol", "Ogrenci");
                                return RedirectToAction("OgrenciPaneli");
                            }
                        }
                        else
                        {
                            ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
                            return View("Index");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Hata: " + ex.Message;
                return View("Index");
            }
        }

        // --- ÖĞRETMEN PANELİ ---
        public IActionResult OgretmenPaneli()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogretmen") return RedirectToAction("Index");

            string yetkiliDers = HttpContext.Session.GetString("YetkiliDers");
            ViewBag.DersAdi = yetkiliDers;
            ViewBag.HocaAdi = HttpContext.Session.GetString("KullaniciAdi");

            List<OgrenciNotu> notListesi = new List<OgrenciNotu>();

            using (SqlConnection baglanti = new SqlConnection(connectionString))
            {
                baglanti.Open();
                string sql = "SELECT * FROM Grades WHERE LessonName = @ders ORDER BY Average DESC";
                SqlCommand komut = new SqlCommand(sql, baglanti);
                komut.Parameters.AddWithValue("@ders", yetkiliDers);

                using (SqlDataReader oku = komut.ExecuteReader())
                {
                    while (oku.Read())
                    {
                        notListesi.Add(new OgrenciNotu
                        {
                            Id = Convert.ToInt32(oku["Id"]),
                            StudentName = oku["StudentName"].ToString(),
                            LessonName = oku["LessonName"].ToString(),
                            ClassLevel = Convert.ToInt32(oku["ClassLevel"]),
                            MidtermExam = Convert.ToDouble(oku["MidtermExam"]),
                            FinalExam = Convert.ToDouble(oku["FinalExam"]),
                            Average = Convert.ToDouble(oku["Average"]),
                            IsPassed = Convert.ToBoolean(oku["IsPassed"]),
                            Attendance = oku["Attendance"] != DBNull.Value ? Convert.ToInt32(oku["Attendance"]) : 0,
                            LetterGrade = oku["LetterGrade"] != DBNull.Value ? oku["LetterGrade"].ToString() : "-"
                        });
                    }
                }
            }
            return View(notListesi);
        }

        // --- ÖĞRENCİ PANELİ ---
        public IActionResult OgrenciPaneli()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Index");

            string ogrAdi = HttpContext.Session.GetString("KullaniciAdi");
            List<OgrenciNotu> notlar = new List<OgrenciNotu>();

            using (SqlConnection baglanti = new SqlConnection(connectionString))
            {
                baglanti.Open();
                string sql = "SELECT * FROM Grades WHERE StudentName = @ad";
                SqlCommand komut = new SqlCommand(sql, baglanti);
                komut.Parameters.AddWithValue("@ad", ogrAdi);

                using (SqlDataReader oku = komut.ExecuteReader())
                {
                    while (oku.Read())
                    {
                        notlar.Add(new OgrenciNotu
                        {
                            Id = Convert.ToInt32(oku["Id"]),
                            StudentName = oku["StudentName"].ToString(),
                            LessonName = oku["LessonName"].ToString(),
                            ClassLevel = Convert.ToInt32(oku["ClassLevel"]),
                            MidtermExam = Convert.ToDouble(oku["MidtermExam"]),
                            FinalExam = Convert.ToDouble(oku["FinalExam"]),
                            Average = Convert.ToDouble(oku["Average"]),
                            IsPassed = Convert.ToBoolean(oku["IsPassed"]),
                            Attendance = oku["Attendance"] != DBNull.Value ? Convert.ToInt32(oku["Attendance"]) : 0,
                            LetterGrade = oku["LetterGrade"] != DBNull.Value ? oku["LetterGrade"].ToString() : "-"
                        });
                    }
                }
            }
            return View(notlar);
        }

        // --- NOT EKLEME ---
        [HttpPost]
        public IActionResult NotEkle(string adSoyad, int sinif, double vize, double final)
        {
            string dersAdi = HttpContext.Session.GetString("YetkiliDers");
            double ortalama = (vize * 0.4) + (final * 0.6);
            bool gectiMi = ortalama >= 50;
            string harf = HesaplaHarf(ortalama);

            try
            {
                using (SqlConnection baglanti = new SqlConnection(connectionString))
                {
                    baglanti.Open();
                    string sql = "INSERT INTO Grades (StudentName, LessonName, ClassLevel, MidtermExam, FinalExam, Average, IsPassed, Attendance, LetterGrade) VALUES (@ad, @ders, @sinif, @vize, @final, @ort, @durum, 0, @harf)";
                    SqlCommand komut = new SqlCommand(sql, baglanti);
                    komut.Parameters.AddWithValue("@ad", adSoyad);
                    komut.Parameters.AddWithValue("@ders", dersAdi);
                    komut.Parameters.AddWithValue("@sinif", sinif);
                    komut.Parameters.AddWithValue("@vize", vize);
                    komut.Parameters.AddWithValue("@final", final);
                    komut.Parameters.AddWithValue("@ort", ortalama);
                    komut.Parameters.AddWithValue("@durum", gectiMi);
                    komut.Parameters.AddWithValue("@harf", harf);
                    komut.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                return Content("HATA: " + ex.Message);
            }
            return RedirectToAction("OgretmenPaneli");
        }

        // --- NOT SİLME ---
        [HttpPost]
        public IActionResult NotSil(int id)
        {
            using (SqlConnection baglanti = new SqlConnection(connectionString))
            {
                baglanti.Open();
                string sql = "DELETE FROM Grades WHERE Id = @id";
                SqlCommand komut = new SqlCommand(sql, baglanti);
                komut.Parameters.AddWithValue("@id", id);
                komut.ExecuteNonQuery();
            }
            return RedirectToAction("OgretmenPaneli");
        }

        // --- DÜZENLEME EKRANI GETİR ---
        [HttpGet]
        public IActionResult NotDuzenle(int id)
        {
            OgrenciNotu ogr = new OgrenciNotu();
            using (SqlConnection baglanti = new SqlConnection(connectionString))
            {
                baglanti.Open();
                string sql = "SELECT * FROM Grades WHERE Id = @id";
                SqlCommand komut = new SqlCommand(sql, baglanti);
                komut.Parameters.AddWithValue("@id", id);
                using (SqlDataReader oku = komut.ExecuteReader())
                {
                    if (oku.Read())
                    {
                        ogr.Id = Convert.ToInt32(oku["Id"]);
                        ogr.StudentName = oku["StudentName"].ToString();
                        ogr.LessonName = oku["LessonName"].ToString();
                        ogr.ClassLevel = Convert.ToInt32(oku["ClassLevel"]);
                        ogr.MidtermExam = Convert.ToDouble(oku["MidtermExam"]);
                        ogr.FinalExam = Convert.ToDouble(oku["FinalExam"]);
                        ogr.Average = Convert.ToDouble(oku["Average"]);
                    }
                }
            }
            return View(ogr);
        }

        // --- NOT GÜNCELLEME İŞLEMİ ---
        [HttpPost]
        public IActionResult NotGuncelle(OgrenciNotu ogr)
        {
            double ort = (ogr.MidtermExam * 0.4) + (ogr.FinalExam * 0.6);
            bool gecti = ort >= 50;
            string harf = HesaplaHarf(ort);

            using (SqlConnection baglanti = new SqlConnection(connectionString))
            {
                baglanti.Open();
                string sql = "UPDATE Grades SET StudentName=@ad, ClassLevel=@sinif, MidtermExam=@vize, FinalExam=@final, Average=@ort, IsPassed=@durum, LetterGrade=@harf WHERE Id=@id";
                SqlCommand komut = new SqlCommand(sql, baglanti);
                komut.Parameters.AddWithValue("@ad", ogr.StudentName);
                komut.Parameters.AddWithValue("@sinif", ogr.ClassLevel);
                komut.Parameters.AddWithValue("@vize", ogr.MidtermExam);
                komut.Parameters.AddWithValue("@final", ogr.FinalExam);
                komut.Parameters.AddWithValue("@ort", ort);
                komut.Parameters.AddWithValue("@durum", gecti);
                komut.Parameters.AddWithValue("@harf", harf);
                komut.Parameters.AddWithValue("@id", ogr.Id);
                komut.ExecuteNonQuery();
            }
            return RedirectToAction("OgretmenPaneli");
        }

        // --- DEVAMSIZLIK GÜNCELLEME ---
        [HttpPost]
        public IActionResult DevamsizlikGuncelle(int ogrenciId, int yeniSaat)
        {
            try 
            {
                using (SqlConnection baglanti = new SqlConnection(connectionString))
                {
                    baglanti.Open();
                    string sorgu = "UPDATE Grades SET Attendance = @saat WHERE Id = @id";
                    using (SqlCommand komut = new SqlCommand(sorgu, baglanti))
                    {
                        komut.Parameters.AddWithValue("@saat", yeniSaat);
                        komut.Parameters.AddWithValue("@id", ogrenciId);
                        komut.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Hata: " + ex.Message);
            }
            return RedirectToAction("OgretmenPaneli");
        }

        // Yardımcı Metot: Harf Notu Hesaplama
        private string HesaplaHarf(double ort)
        {
            if (ort >= 90) return "AA";
            if (ort >= 85) return "BA";
            if (ort >= 80) return "BB";
            if (ort >= 75) return "CB";
            if (ort >= 70) return "CC";
            if (ort >= 65) return "DC";
            if (ort >= 60) return "DD";
            if (ort >= 50) return "FD";
            return "FF";
        }
    }
}