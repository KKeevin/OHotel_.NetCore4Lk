/*----------------- Main Declaration -----------------*/
const webApiBaseAddress = "https://localhost:7015";
const token = localStorage.getItem('Token');

/*----------------------------------------------------*/

//***********************************************//
//*                   Layout                    *//                         容 器
//***********************************************//--------------------------------------------------------
const LayoutVue = Vue.createApp({
    data() {
        return {
            ManageClassData: null,
            currentController: null,
            currentAction: null,
        };
    },
    mounted() {
        this.currentController = getCurrentControllerAndAction()?.controller;
        this.currentAction = getCurrentControllerAndAction()?.action;
        this.getManageClassData();
    },
    methods: {
        getManageClassData() {
            const token = localStorage.getItem('Token');
            if (!token) {
                this.ManageClassData = {};
                return;
            }
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: { "Authorization": `Bearer ${token}` }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    if (!STNo) {
                        console.warn('GetUserInfo 未回傳 STNo，嘗試 STNo=1');
                        this.tryFallbackMenuFetch(token);
                        return;
                    }
                    axios.get(`/api/Layout/GetManageClassAndItemsBySTNo/${STNo}`, {
                        headers: { "Authorization": `Bearer ${token}` }
                    })
                        .then(response => {
                            this.ManageClassData = response.data || {};
                            this.setTreeViewActive();
                        })
                        .catch(error => {
                            console.warn('GetManageClassAndItemsBySTNo 失敗', error.response?.status, error.response?.data);
                            this.ManageClassData = {};
                        });
                })
                .catch(error => {
                    console.warn('GetUserInfo 失敗', error.response?.status, error.response?.data);
                    this.tryFallbackMenuFetch(token);
                });
        },
        tryFallbackMenuFetch(token) {
            if (!token) { this.ManageClassData = {}; return; }
            axios.get('/api/Layout/GetManageClassAndItemsBySTNo/1', { headers: { "Authorization": `Bearer ${token}` } })
                .then(r => { this.ManageClassData = r.data || {}; this.setTreeViewActive(); })
                .catch(() => { this.ManageClassData = {}; });
        },
        seedMenu() {
            axios.post('/api/DbInit/SeedSqliteMenu')
                .then(() => {
                    alert('選單已初始化，請重新整理頁面');
                    window.location.reload();
                })
                .catch(err => {
                    alert('初始化失敗：' + (err.response?.data?.message || err.message));
                });
        },
        setTreeViewActive() {
            const currentController = this.currentController;
            if (!currentController) {
                return;
            }
            // 找到對應的主類別，加上 is-expanded 樣式
            $('.treeview').filter(function () {
                return $(this).find('[data-controller]').data('controller') === currentController;
            }).addClass('is-expanded');
        },
        isMenuItemActive(item) { // 找到對應的選單項目，加上 active 樣式
            const currentAction = this.currentAction;
            return item.MIAction === currentAction;
        },
        expand(controller) {
            const currentTreeview = $(`[data-controller="${controller}"]`).parents('.treeview');
            $('.treeview').not(currentTreeview).removeClass('is-expanded');
            currentTreeview.toggleClass('is-expanded');
        },
    },
}).mount('#LayoutVue');

//***********************************************//
//*                   Header                    *//                         頁 眉
//***********************************************//--------------------------------------------------------
const HeaderVue = Vue.createApp({
    data() {
        return {
            STName: null,
        };
    },
    mounted() {
        this.getSTNameByToken();
    },
    methods: {
        logout: function () {
            localStorage.removeItem('Token');
            window.location.href = '/Sys/login';
        },
        getSTNameByToken() {
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetSTNameBySTNo/${STNo}`, {
                        headers: {
                            "Authorization": `Bearer ${token}`
                        }
                    })
                        .then(response => {
                            this.STName = response.data;
                        })
                        .catch(error => {
                            this.STName = 'Unknown'
                        });
                })
                .catch(error => {
                    this.STName = 'Unknown#'
                });
        },
    },
}).mount('#HeaderVue');

//***********************************************//
//*                    Login                    *//                         登 入
//***********************************************//--------------------------------------------------------
const LoginVue = Vue.createApp({
    data() {
        return {
            account: '',
            password: '',
            inputCaptcha: '',
            captcha: '',
        };
    },
    mounted() {
        this.refreshCaptcha();
    },
    methods: {
        checkLogin() {
            const isDev = /localhost|127\.0\.0\.1/.test(window.location.hostname);
            if (isDev || this.inputCaptcha === this.captcha) {
                this.Login();
            } else {
                alert("驗證碼有誤!!");
                this.refreshCaptcha();
            }
        },
        Login: async function () {
            try {
                const formData = new FormData();
                formData.append('LoginName', this.account);
                formData.append('LoginPasswd', this.password);

                const response = await axios.post(`/api/AccountLogin/Login`, formData);

                if (response.status === 200) {
                    const token = response.data;
                    localStorage.setItem("Token", token);

                    alert("登入成功");
                    window.location = "/Sys";
                } else {
                    alert("帳號或密碼有誤");
                    window.location = "/Sys/Login";
                }
            } catch (error) {
                const msg = error.response?.data ?? error.message ?? "登入失敗，請重新輸入";
                alert(typeof msg === "string" ? msg : "登入失敗，請重新輸入");
            }
        },
        refreshCaptcha() {
            this.captcha = this.generateCaptcha();
            this.drawCaptcha();
            this.inputCaptcha = '';
        },
        generateCaptcha() {
            const characters = 'abcdefghijklmnopqrstuvwxyz0123456789';
            let captcha = '';

            for (let i = 0; i < 5; i++) {
                const randomIndex = Math.floor(Math.random() * characters.length);
                captcha += characters.charAt(randomIndex);
            }

            return captcha;
        },
        drawCaptcha() {
            const canvas = this.$refs.captchaCanvas;
            const ctx = canvas.getContext('2d');

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.fillStyle = 'LightYellow';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.font = 'bold 18px Verdana';
            ctx.fillStyle = 'black';
            ctx.textBaseline = 'middle';
            ctx.textAlign = 'center';

            const textX = canvas.width / 2;
            const textY = canvas.height / 2;
            ctx.fillText(this.captcha, textX, textY);
        },
    },
}).mount("#LoginVue");

//***********************************************//
//*                    Pages                    *//                       頁 面 們
//***********************************************//--------------------------------------------------------

// --------------------------------------------- VUE End ---------------------------------------------