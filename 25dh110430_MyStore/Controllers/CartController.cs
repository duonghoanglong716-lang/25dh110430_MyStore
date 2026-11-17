using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using _25dh110430_MyStore.Models;
using _25dh110430_MyStore.Models.ViewModel;
using PagedList;

namespace _25dh110430_MyStore.Controllers
{
    public class CartController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();



        // Hàm lấy dịch vụ giỏ hàng
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // Hiển thị giỏ hàng không gom nhóm theo danh mục
        // Hiển thị giỏ hàng
        public ActionResult Index()
        {
            var cart = GetCartService().GetCart();
            return View(cart); // Phải chắc chắn trả về đối tượng cart có kiểu Cart
        }

        // Thêm sản phẩm vào giỏ hàng

        [HttpPost]
        public ActionResult AddToCart(int id, int quantity = 1, int? productColorId = null)
        {
            var product = db.Products.Find(id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index", "Home");
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng không hợp lệ!";
                return RedirectToAction("ProductDetails", "Home", new { id = id });
            }

            if (product.StockQuantity <= 0)
            {
                TempData["Error"] = "Sản phẩm đã hết hàng!";
                return RedirectToAction("ProductDetails", "Home", new { id = id });
            }

            // Lấy giỏ hàng hiện tại
            var cartService = GetCartService();
            var cart = cartService.GetCart();

            // Kiểm tra màu sắc
            string colorName = null;
            string productImage = product.ProductImage;

            if (productColorId.HasValue)
            {
                var selectedColor = db.ProductColors
                    .Include("ProductImages")
                    .FirstOrDefault(c =>
                        c.ProductColorID == productColorId.Value &&
                        c.ProductID == product.ProductID);

                if (selectedColor == null)
                {
                    TempData["Error"] = "Màu sắc không hợp lệ!";
                    return RedirectToAction("ProductDetails", "Home", new { id = id });
                }

                colorName = selectedColor.ColorName;

                var colorImage = selectedColor.ProductImages.FirstOrDefault();

                if (colorImage != null &&
                    !String.IsNullOrEmpty(colorImage.ImagePath))
                {
                    productImage = colorImage.ImagePath;
                }
            }
            else if (db.ProductColors.Any(c => c.ProductID == product.ProductID))
            {
                TempData["Error"] = "Vui lòng chọn màu sắc!";
                return RedirectToAction("ProductDetails", "Home", new { id = id });
            }

            // Tìm sản phẩm cùng mã và cùng màu đã có trong giỏ
            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductID == product.ProductID &&
                i.ProductColorID == productColorId);

            // Tính tổng số lượng sau khi thêm
            int currentQuantity = existingItem != null
                ? existingItem.Quantity
                : 0;

            if (currentQuantity + quantity > product.StockQuantity)
            {
                TempData["Error"] = "Số lượng trong giỏ vượt quá số lượng tồn kho!";
                return RedirectToAction("ProductDetails", "Home", new { id = id });
            }

            // Thêm sản phẩm vào giỏ
            cart.AddItem(
                product.ProductID,
                productImage,
                product.ProductName,
                product.ProductPrice,
                quantity,
                product.Category.CategoryName,
                productColorId,
                colorName
            );

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index");
        }




        // Xóa sản phẩm khỏi giỏ
        public ActionResult RemoveFromCart(int id)
        {
            var cartService = GetCartService();
            cartService.GetCart().RemoveItem(id);
            return RedirectToAction("Index");
        }

        // Làm trống giỏ hàng
        public ActionResult ClearCart()
        {
            GetCartService().ClearCart();
            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult UpdateQuantity(
            int id,
            int quantity,
            int? productColorId = null)
        {
            var product = db.Products.Find(id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index");
            }

            if (quantity < 1)
            {
                TempData["Error"] = "Số lượng không hợp lệ!";
                return RedirectToAction("Index");
            }

            if (quantity > product.StockQuantity)
            {
                TempData["Error"] = "Số lượng vượt quá tồn kho!";
                return RedirectToAction("Index");
            }

            var cart = GetCartService().GetCart();

            cart.UpdateQuantity(id, quantity, productColorId);

            TempData["Success"] = "Đã cập nhật số lượng sản phẩm!";
            return RedirectToAction("Index");
        }


        // Hiển thị giỏ hàng đã gom nhóm sản phẩm theo danh mục và hỗ trợ sản phẩm tương tự/phân trang
        public ActionResult Index2(int? page)
        {
            var cart = GetCartService().GetCart();
            var products = db.Products.ToList();
            var similarProducts = new List<Product>();

            if (cart.Items != null && cart.Items.Any())
            {
                similarProducts = products.Where(p => cart.Items.Any(ci => ci.Category == p.Category.CategoryName)
                    && !cart.Items.Any(ci => ci.ProductID == p.ProductID)).ToList();
            }

            // Đoạn code liên quan tới phân trang
            // Lấy số trang hiện tại (mặc định là trang 1 nếu không có giá trị)
            int pageNumber = page ?? 1;
            int pageSize = cart.PageSize;

            cart.SimilarProducts = similarProducts
                .OrderBy(p => p.ProductID)
                .ToPagedList(pageNumber, pageSize);

            return View(cart);
        }
    }
}
