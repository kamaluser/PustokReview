/*$(document).ready(function () {

    $(".book-modal").click(function (e) {
        console.log("das00");
        e.preventDefault();
        let url = this.getAttribute("href");

        fetch(url)
            .then(response => response.text())
            .then(data => {
                $("#quickModal .modal-dialog").html(data)
            })

        $("#quickModal").modal('show');
    })
})*/



$(document).ready(function () {

    $(".book-modal").click(function (e) {
        e.preventDefault();
        let url = this.getAttribute("href");

        fetch(url)
            .then(response => response.text())
            .then(data => {
                $("#quickModal .modal-dialog").html(data)
            })

        $("#quickModal").modal('show');
    })
})

document.querySelectorAll(".add-basket").forEach(function (element) {
    element.addEventListener("click", function (event) {
        event.preventDefault();

        var url = element.getAttribute("href");
        fetch(url)
            .then(response => response.json())
            .then(data => {
                if (data.isSucceeded === false) {
                    alert('Exception occurred!');
                } else {
                    alert('Added to basket!');
                    document.querySelector(".basket-count").textContent = data.totalCount;
                    document.querySelector(".basket-price").textContent = "£" + data.totalPrice;
                }
            });
    });
});





$(document).on("click", ".load-comments", function (e) {
    e.preventDefault();

    var nextPage = $(this).attr("data-nextpage");
    var url = $(this).attr("href") + "?page=" + nextPage;

    fetch(url)
        .then(response => response.text())
        .then(data => {
            $(".reviews").append(data);
        });

    var totalPage = +$(this).data("totalpage");
    nextPage++;

    if (nextPage > totalPage) {
        $(this).remove();
    }
    $(this).attr("data-nextpage", nextPage);
});
