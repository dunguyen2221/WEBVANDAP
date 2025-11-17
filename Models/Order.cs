// File: Models/Order.cs
namespace WEBVANDAP.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            this.OrderItems = new HashSet<OrderItem>();
        }

        public int Id { get; set; }
        public string OrderCode { get; set; }
        public Nullable<System.DateTime> OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public Nullable<bool> IsPaid { get; set; }
        public string UserId { get; set; }
        public Nullable<int> ShippingAddressId { get; set; }

        // === FIX: THÊM THU?C TÍNH NÀY VÀO ÐÂY ===
        public string Notes { get; set; }
        // ======================================

        public virtual Address Address { get; set; }
        public virtual AspNetUser AspNetUser { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}