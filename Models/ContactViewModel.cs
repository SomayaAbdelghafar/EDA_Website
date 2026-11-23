using System.ComponentModel.DataAnnotations;

namespace EDA_Website.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "يرجى إدخال الاسم")]
    [StringLength(100, ErrorMessage = "الاسم طويل جدًا")]
    [Display(Name = "الاسم")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "يرجى إدخال رقم الجوال")]
    [RegularExpression(@"^[0-9+\s\-]{9,16}$", ErrorMessage = "يرجى إدخال رقم جوال صحيح")]
    [Display(Name = "رقم الجوال")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "يرجى إدخال بريد إلكتروني صحيح")]
    [StringLength(150)]
    [Display(Name = "البريد الإلكتروني (اختياري)")]
    public string? Email { get; set; }

    [Display(Name = "الخدمة المطلوبة")]
    public string? Service { get; set; }

    [StringLength(100)]
    [Display(Name = "المدينة")]
    public string? City { get; set; }

    [Required(ErrorMessage = "يرجى كتابة تفاصيل الطلب")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "يرجى كتابة 10 أحرف على الأقل")]
    [Display(Name = "تفاصيل الطلب")]
    public string Message { get; set; } = "";
}
