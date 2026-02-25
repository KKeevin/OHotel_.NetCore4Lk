// 取得目前網址的 controller 與 action
function getCurrentControllerAndAction() {
    const path = window.location.pathname;
    if (!path) return null;
    const parts = path.split('/').filter(Boolean);
    let controller = parts[1];
    let action = parts[2] || null;
    if (controller && controller.toLowerCase() === 'sys') {
        controller = parts[2];
        action = parts[3] || null;
    }
    return controller ? { controller, action } : null;
}

// [小工具] Alert要顯示什麼, 多久顯示
function showAlert(message, delay) {
    setTimeout(function () {
        alert(message);
    }, delay);
}

window.onload = function () {
    // Toggle Sidebar
    $('[data-toggle="sidebar"]').click(function (event) {
        event.preventDefault();
        $('.app').toggleClass('sidenav-toggled');
    });

    /* 判斷 localStorage 的 Token 跳轉為登入頁面與否 */
    const token = localStorage.getItem('Token');
    const loginPageUrl = '/Sys/Login';
    const homePageUrl = '/Sys/Welcome';

    if (!token || token === 'undefined') {
        if (window.location.pathname !== loginPageUrl) {
            window.location.href = loginPageUrl;
        }
    } else {
        axios.get(`/api/AccountLogin/GetUserInfo`, {
            headers: {
                "Authorization": `Bearer ${token}`
            }
        })
            .then(response => {
                if (window.location.pathname === loginPageUrl) {
                    window.location.href = homePageUrl;
                }
            })
            .catch(error => {
                console.log(error);
                if (window.location.pathname !== loginPageUrl) {
                    window.location.href = loginPageUrl;
                }
            });
    }
};

//-------------------------------------------
//               Data Rendering
//-------------------------------------------
$(function () {
    //--快速排序--開始

    //--點選後產生顏色,並賦予li_selected class name
    $(document).on("click", ".order_li", function () {
        $index = $(".order_li").index($(this));
        $(this).addClass("li_selected");
        $(this).siblings().removeClass("li_selected");
    });

    //手動--向上移動
    $(document).on("click", "#list_up", function () {
        if ($(".li_selected").length > 0) {
            $index = $(".order_li").index($(".li_selected"));//選擇的項目位置
            if ($index > 0) {
                //--順序排列 向上
                $(".order_li").eq($index).insertBefore($(".order_li").eq($index - 1));
                $("#scroll_list ul").stop().animate({
                    scrollTop: $(".order_li").height() * ($index - 1)
                });
            }
        }
    });
    //手動--向下移動
    $(document).on("click", "#list_down", function () {
        if ($(".li_selected").length > 0) {
            $index = $(".order_li").index($(".li_selected"));

            //--順序排列 向下
            $(".order_li").eq($index).insertAfter($(".order_li").eq($index + 1));
            $("#scroll_list ul").stop().animate({
                scrollTop: $(".order_li").height() * ($index + 1)
            });
        }
    });
    // 確認修改
    $(document).on("click", "#order_ok", function () {
        var Tmp = "";
        var all_size = $(".order_li").length;
        if (all_size > 0) {
            for (var i = 0; i < all_size; i++) {
                Tmp = Tmp + $(".order_li").eq(i).attr("data-no") + ",";
            }
            Tmp = Tmp + "-1";
            var Action = $("form[name='form1']").attr("action");
            Action = Action.substring(0, Action.lastIndexOf("_")) + "_.aspx";
            sys.RFastOrder("form1", Action, Tmp);//送出資訊
            //window.close();
        } else {
            alert("很抱歉,列表無資料");
            return false;
        }
    });
})