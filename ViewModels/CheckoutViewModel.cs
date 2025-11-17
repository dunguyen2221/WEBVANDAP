using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WEBVANDAP.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(100)]
        public string ShippingFullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string ShippingPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết.")]
        [StringLength(255)]
        public string ShippingStreet { get; set; }

        // 2. TÙY CHỌN ĐỊA CHỈ ĐÃ LƯU
        // ID Địa chỉ đã có (sẽ null nếu người dùng nhập mới)
        public int? SelectedAddressId { get; set; }

        // 3. TÙY CHỌN THANH TOÁN & GHI CHÚ
        public string PaymentMethod { get; set; } // Ví dụ: "COD", "Online"
        public string Notes { get; set; }

        // 4. DỮ LIỆU TÍNH TOÁN (Controller sẽ điền)
        public decimal CartTotal { get; set; }
        public decimal ShippingFee { get; set; } = 30000; // Phí ship giả định

        public decimal FinalTotal => CartTotal + ShippingFee;
    }
}