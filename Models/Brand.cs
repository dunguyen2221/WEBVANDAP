// Trong WEBVANDAP.Models/Brand.cs
// ... (code Entity Framework generated) ...
using System.ComponentModel.DataAnnotations; // Đảm bảo có using này

namespace WEBVANDAP.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Brand
    {
        // ... (Constructor) ...

        // ******************************************************
        // KHẮC PHỤC LỖI: Thêm lại Khóa chính Id
        // ******************************************************
        public int Id { get; set; }

        // Khai báo Name và các thuộc tính khác (giữ nguyên validation)
        [Required(ErrorMessage = "Tên thương hiệu không được để trống")]
        [StringLength(255)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        public string Slug { get; set; }
        public string Logo { get; set; }
        public Nullable<bool> IsActive { get; set; }

        public virtual Category Category { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Product> Products { get; set; }
    }
}