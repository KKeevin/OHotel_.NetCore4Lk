/*----------------- Main Declaration -----------------*/
const webApiBaseAddress = "https://localhost:7015";

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

            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/Layout/GetManageClassAndItemsBySTNo/${STNo}`)
                        .then(response => {
                            this.ManageClassData = response.data;
                            this.setTreeViewActive();
                        })
                        .catch(error => {
                            console.log(error);
                        });
                })
                .catch(error => {
                    console.log(error);
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
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetSTNameBySTNo/${STNo}`)
                        .then(response => {
                            this.STName = response.data;
                        })
                        .catch(error => {
                            this.STName = 'Unknown'
                            //    console.log(error);
                        });
                })
                .catch(error => {
                    this.STName = 'Unknown#'
                    //    console.log(error);
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
            if (this.inputCaptcha === this.captcha) {
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
                alert("登入失敗，請重新輸入");
                window.location = "/Sys/Login";
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

// 【 Test.cshtml 】
const CaptchaVue = Vue.createApp({
    data() {
        return {
            captcha: '',
            inputCaptcha: '',
            showMessage: false,
            message: '',
        };
    },
    created() {
        this.refreshCaptcha();
    },
    mounted() {
        this.drawCaptcha();
    },
    methods: {
        generateCaptcha() {
            const characters = 'abcdefghijklmnopqrstuvwxyz0123456789';
            let captcha = '';

            for (let i = 0; i < 5; i++) {
                const randomIndex = Math.floor(Math.random() * characters.length);
                captcha += characters.charAt(randomIndex);
            }

            return captcha;
        },
        refreshCaptcha() {
            this.captcha = this.generateCaptcha();
            this.drawCaptcha();
            this.inputCaptcha = '';
            this.showMessage = false;
            this.message = '';
        },
        drawCaptcha() {
            const canvas = this.$refs.captchaCanvas;
            const ctx = canvas.getContext('2d');

            // 清空畫布
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // 繪製底色
            ctx.fillStyle = 'LightYellow';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // 設定字體
            ctx.font = 'bold 18px Verdana';
            ctx.fillStyle = 'black';
            ctx.textBaseline = 'middle';
            ctx.textAlign = 'center';

            // 繪製文字
            const textX = canvas.width / 2;
            const textY = canvas.height / 2;
            ctx.fillText(this.captcha, textX, textY);
        },
        checkCaptcha() {
            if (this.inputCaptcha === this.captcha) {
                this.showMessage = true;
                this.message = '驗證碼正確';
            } else {
                this.refreshCaptcha();
            }
        },
    },
}).mount("#CaptchaVue");

// 【 System/Class.cshtml 】
const ClassVue = Vue.createApp({
    data() {
        return {
            // 主要顯示資料存放區 //
            ManageClassData: null,
            // 現在頁面 & 導覽列  //
            contentNow: 1,
            breadcrumb: {
                1: '',
                2: '-新增',
                3: '-修改',
            },
            //   新增 & 編輯   //
            addData: {},
            editData: {},
            editMCNo: null,
            //  放在  AXIOS 傳遞 //
            pageSize: 4, //一頁幾筆資料
            pageNow: 1,
            Sort: 'ASC',
            Column: '%5BOrder%5D',
            /* ------------------ */
            pageTotal: null,
            selectedItems: [],
            /* ------------------ */
            PV: 0,
            PA: 0,
            PD: 0,
            PU: 0,
            PG: 0,
            P1: 0,
            P2: 0,
        };
    },
    created() {
        // 設置預設值
        this.addData.mCIcon = 'fa fa-cog';
        this.addData.order = '0';
    },
    mounted() {
        let _this = this;
        _this.getAndSearchManageClassData(_this.pageNow);
        _this.linkGetPage();
        _this.getPermissionValue('PV');
        _this.getPermissionValue('PA');
        _this.getPermissionValue('PD');
        _this.getPermissionValue('PU');
        _this.getPermissionValue('PG');
        _this.getPermissionValue('P1');
        _this.getPermissionValue('P2');
    },
    methods: {
        // << 搜尋 & 資料渲染(分頁) >>
        getAndSearchManageClassData(pageNow, searchItem = '', searchColumn = 'MCName') {
            // 中文、英文以外空格、特殊符號都刪除
            searchItem = searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();

            let apiUrl = '';
            if (searchItem === '') {
                apiUrl = `/api/SystemClass/GetManageClass_Paged?page=${pageNow}&pageSize=${this.pageSize}&selectFrom=ManageClass&orderBy=${this.Column}%20${this.Sort}`;
            } else {
                apiUrl = `/api/SystemClass/SearchManageClass_Paged/SearchPaged?search=${searchItem}&column=${searchColumn}&page=${pageNow}&pageSize=${this.pageSize}&selectFrom=ManageClass&orderBy=${this.Column}%20${this.Sort}`;
            }

            axios.get(apiUrl)
                .then(response => {
                    this.pageTotal = response.data.Paging;
                    this.ManageClassData = response.data.Data;
                    this.searchItem = searchItem;
                    this.searchColumn = searchColumn;
                    this.pageNow = pageNow;
                })
                .catch(error => {
                    console.log(error);
                });
        },
        // << 切換分頁 >>
        goToPage(page) {
            if (this.searchItem === '') {
                this.getAndSearchManageClassData(page);
            } else {
                this.getAndSearchManageClassData(page, this.searchItem, this.searchColumn);
            }
            // 切換分頁時取消全選
            let checkAll = document.getElementById("checkAll");
            checkAll.checked = false;
            this.checkAll(); // 調用checkAll方法，將複選框狀態更新到selectedItems陣列。
        },
        // --------------------------- 新刪修 資料夾/檔案 ----------------------------------
        // [ 建立新資料夾至 Views ]
        CreateFolder(folderName) {
            axios.post(`/api/Tools/CreateFolder/${folderName}`)
        },
        // [ 創建`{folderName}Controller.cs`至 Controllers 裡 ]
        CreateControllerFile(folderName) {
            axios.post(`/api/Tools/CreateControllerFile/${folderName}`)
        },
        // [ 更改 Views 裡現有的資料夾名稱 ]
        EditFolderName(oldFolderName = 'Cacoo', newFolderName = 'Popiiii') {
            axios.patch(`/api/Tools/EditFolderName/${oldFolderName}/${newFolderName}`)
        },
        // [ 查看 Views 裡是否有${folderName}資料夾 ]
        CheckFolderExists(folderName = 'HappyDog') {
            axios.get(`/api/Tools/CheckFolderExists/${folderName}`)
                .then(response => {
                    console.log(folderName + "資料夾存在嗎?: " + response.data);
                })
                .catch(error => {
                    console.error(error);
                });
        },
        // --------------------------------------------------------------------------------
        // [ 資料排序 ]
        sortBy(column) {
            let _this = this;
            if (_this.Sort === 'DESC') {
                _this.Sort = 'ASC';
            } else {
                _this.Sort = 'DESC';
            }
            _this.Column = column;
            console.log(_this.Sort);
            // 以下是在同時確認目前狀態，是搜尋模式還是全部列表
            if (this.searchItem === '') {
                _this.getAndSearchManageClassData(_this.pageNow);
            } else {
                _this.searchItem = _this.searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();
                _this.getAndSearchManageClassData(1, _this.searchItem, _this.searchColumn);
            }
        },
        // [ 時間格式轉換 ]
        formatTime(timeStr) {
            const date = new Date(timeStr);
            if (isNaN(date.getTime())) {
                // 時間格式不正確，返回空字串
                return '';
            }
            const year = date.getFullYear();
            const month = ('0' + (date.getMonth() + 1)).slice(-2);
            const day = ('0' + date.getDate()).slice(-2);
            const hours = ('0' + date.getHours()).slice(-2);
            const minutes = ('0' + date.getMinutes()).slice(-2);
            const seconds = ('0' + date.getSeconds()).slice(-2);
            return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
        },
        // [ 獲取連結的小物 (測試) ]
        linkGetPage: async function () {
            let _this = this;
            var getUrlString = location.href;
            var url = new URL(getUrlString);
            //console.log(url.searchParams.get('page'));
            pageNow = url.searchParams.get('page');
            searchItem = url.searchParams.get('search');
            if (pageNow === null) {
                pageNow = 1;
            }
            _this.getAndSearchManageClassData(pageNow);
        },
        // [ 切換頁面Content內容 (C,R,U,D) & 獲取目前編輯資料]
        content(Num, MCNo) {
            let _this = this;
            _this.contentNow = Num;
            // ↓取得目前編輯資料
            if (MCNo != null) {
                axios.get(`/api/SystemClass/GetManageClass_ByMCNo/${MCNo}`).then(
                    response => {
                        let _this = this;
                        _this.editMCNo = MCNo;
                        _this.editData = response.data;
                    }
                ).catch(
                    error => {
                        console.log(error);
                    }
                );
            }
        },
        // [ 頁碼產生 ]
        generatePagination() {
            let _this = this;
            let pages = [];
            let startPage, endPage;
            let totalPages = this.pageTotal.TotalPages;
            _this.pageNow = this.pageTotal.CurrentPage;

            if (totalPages <= 5) {
                startPage = 1;
                endPage = totalPages;
            } else {
                if (_this.pageNow <= 3) {
                    startPage = 1;
                    endPage = 5;
                } else if (_this.pageNow + 1 >= totalPages) {
                    startPage = totalPages - 4;
                    endPage = totalPages;
                } else {
                    startPage = _this.pageNow - 2;
                    endPage = _this.pageNow + 2;
                }
            }

            for (let i = startPage; i <= endPage; i++) {
                pages.push(i);
            }

            return pages;
        },
        // ----------------------------------------------------
        // 【 新增 】
        create() {
            const request = {
                MCName: this.addData.mCName,
                MCIcon: this.addData.mCIcon,
                MCXtrol: this.addData.mCXtrol,
                MSNo: 1,
                State: 0,
                Order: this.addData.order
            };
            axios.post(`/api/SystemClass/AddManageClass`, request).then(
                response => {
                    _this.CreateControllerFile(_this.addData.mCXtrol)
                    _this.CreateFolder(_this.addData.mCXtrol);
                    alert("新增成功");
                    window.location = `/System/Class/`
                }
            ).catch(error => alert('您有未完成的內容或填寫錯誤'));
        },
        // ----------------------------------------------------
        // 【 修改 】
        update() {
            const request = {
                MCName: this.editData[0].MCName,
                MCXtrol: this.editData[0].MCXtrol,
                MCIcon: this.editData[0].MCIcon,
                Order: this.editData[0].Order
            };
            axios.patch(`/api/SystemClass/UpdateManageClass/${this.editMCNo}`, request).then(
                () => {
                    alert("修改成功");
                    window.location = "/System/Class/";
                }
            ).catch(error => alert("請重新操作"));
        },
        // ----------------------------------------------------
        // 【 刪除 】
        deleteSelected() {
            if (this.selectAll) {
                this.checkAll();
            }
            // 取得被勾選的項目
            const selectedItems = this.selectedItems;

            // 如果沒有項目被勾選，結束函數
            if (selectedItems.length === 0) {
                alert('您沒有選擇要刪除的資料')
                return;
            }

            // 將勾選的項目編號做成一個字串
            const mcnoParams = selectedItems.map(item => `MCNo=${item}`).join('&');

            // 呼叫 deleteManageClassData 方法
            this.deleteData(mcnoParams)
                .then(() => {
                    // 清空 selectedItems 陣列
                    this.selectedItems = [];
                })
                .catch((error) => {
                    // 刪除失敗，顯示錯誤訊息
                    console.error(error);
                });
        },
        /* 批次刪除資料 */
        deleteData(mcnoParams) {
            //console.log(this.selectedItems);
            if (confirm("確定要刪除所選擇的資料嗎？")) {
                axios.delete(`/api/SystemClass/DeleteManageClassBatch/batch?${mcnoParams}`)
                    .then(response => {
                        this.getAndSearchManageClassData(this.pageNow);
                    })
                    .catch(error => {
                        alert('發生不可預期的錯誤');
                    });
            }
        },
        /* 全選 */
        checkAll() {
            let delCodes = document.getElementsByName("del_code");
            let checkAll = document.getElementById("checkAll");
            for (let i = 0; i < delCodes.length; i++) {
                delCodes[i].checked = checkAll.checked;
            }
            // 更新選取項目的陣列
            this.selectedItems = checkAll.checked ? this.ManageClassData.map(item => item.MCNo) : [];
        },
        // ----------------------------------------------------
        // 【 權限 】
        getPermissionValue(permiss, minNo = '1') {
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetStaffPermissions/${STNo}`)
                        .then(response => {
                            const permissions = response.data;
                            const permission = permissions.find(p => p.MINo === parseInt(minNo, 10));
                            if (permission) {
                                this.permissionValue = permission
                                this.$data[permiss] = this.permissionValue[permiss];
                            } else {
                                this.permissionValue = null;
                                this.$data[permiss] = this.permissionValue;
                            }
                        })
                        .catch(error => {
                            console.error('Error:', error);
                        });
                })
                .catch(error => {
                    console.log(error);
                });
        },
    },
}).mount("#ClassVue");

// 【 System/Item.cshtml 】
const ItemVue = Vue.createApp({
    data() {
        return {
            // 主要顯示資料存放區 //
            RenderingData: null,
            // 現在頁面 & 導覽列  //
            contentNow: 1,
            breadcrumb: {
                1: '',
                2: '-新增',
                3: '-修改',
            },
            //   新增 & 編輯   //
            addData: {},
            editData: {},
            editMINo: null,
            //  放在  AXIOS 傳遞 //
            pageSize: 10, //一頁幾筆資料
            pageNow: 1,
            Sort: 'ASC',
            Column: '%5BOrder%5D',
            /* ------------------ */
            pageTotal: null,
            selectedItems: [],
            /* ------------------ */
            SysClassData: null,
            /* ------------------ */
            PV: 0,
            PA: 0,
            PD: 0,
            PU: 0,
            PG: 0,
            P1: 0,
            P2: 0,
        };
    },
    created() {
        // 設置預設值: 新增
        this.addData = {
            mCNo: 0,
            powerView: 0,
            powerAdd: 0,
            powerDel: 0,
            powerUpdate: 0,
            powerGrant: 0,
            power1: '',
            power2: '',
            order: '0'
        };
    },
    mounted() {
        let _this = this;
        _this.getDataPaged(_this.pageNow);
        _this.getSysClassData();
        _this.linkGetPage();
        _this.getPermissionValue('PV');
        _this.getPermissionValue('PA');
        _this.getPermissionValue('PD');
        _this.getPermissionValue('PU');
        _this.getPermissionValue('PG');
        _this.getPermissionValue('P1');
        _this.getPermissionValue('P2');
    },
    methods: {
        // << 搜尋 & 資料渲染(分頁) >>
        getDataPaged(pageNow, searchItem = '', searchColumn = 'ItemName') {
            // 中文、英文以外空格、特殊符號都刪除
            searchItem = searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();

            let apiUrl = `/api/SystemItem/GetPaged?`;
            if (searchItem === '') {
                apiUrl += `page=${pageNow}&pageSize=${this.pageSize}&selectFrom=ManageItem&orderBy=${this.Column}%20${this.Sort}`;
            } else {
                apiUrl += `search=${searchItem}&column=${searchColumn}&page=${pageNow}&pageSize=${this.pageSize}&selectFrom=ManageItem&orderBy=${this.Column}%20${this.Sort}`;
            }

            // 獲取 localStorage 中的 Token
            const token = localStorage.getItem('Token');

            axios.get(apiUrl, {
                headers: {
                    Authorization: `Bearer ${token}` // 將 Token 放入標頭
                }
            })
                .then(response => {
                    this.pageTotal = response.data.Paging;
                    this.RenderingData = response.data.Data;
                    this.searchItem = searchItem;
                    this.searchColumn = searchColumn;
                    this.pageNow = pageNow;
                })
                .catch(error => {
                    console.log(error);
                });
        },
        // < 獲得控制器資料 >
        getSysClassData() {
            let _this = this;
            axios.get(`/api/SystemClass/GetManageClass/`).then(
                response => {
                    _this.SysClassData = response.data;
                }
            );
        },
        // < 由 MCNo 獲取 Controller 名稱 >
        getMCXtrolByMCNo(MCNo) {
            let foundMC = this.SysClassData.find(item => item.MCNo === MCNo);
            if (foundMC) {
                return foundMC.MCXtrol;
            } else {
                return null;
            }
        },
        // << 切換分頁 >>
        goToPage(page) {
            if (this.searchItem === '') {
                this.getDataPaged(page);
            } else {
                this.getDataPaged(page, this.searchItem, this.searchColumn);
            }
            // 切換分頁時取消全選
            let checkAll = document.getElementById("checkAll");
            checkAll.checked = false;
            this.checkAll(); // 調用checkAll方法，將複選框狀態更新到selectedItems陣列。
        },
        // --------------------------- 新刪修 資料夾/檔案 ----------------------------------
        // [ 至 Views 裡{folderName}創建{fileName).cshtml文件 ]
        CreateCshtmlFile(folderName, fileName) {
            axios.post(`/api/Tools/CreateCshtmlFile/${folderName}/${fileName}`)
        },
        // [ 至`{folderName}Controller.cs`裡面增加新的{fileName} View Action ]
        AddViewAction(folderName, fileName) {
            axios.post(`/api/Tools/AddViewAction/${folderName}/${fileName}`)
        },
        // --------------------------------------------------------------------------------
        // [ 資料排序 ]
        sortBy(column) {
            let _this = this;
            if (_this.Sort === 'DESC') {
                _this.Sort = 'ASC';
            } else {
                _this.Sort = 'DESC';
            }
            _this.Column = column;
            //console.log(_this.Sort);
            // 以下是在同時確認目前狀態，是搜尋模式還是全部列表
            if (this.searchItem === '') {
                _this.getDataPaged(_this.pageNow);
            } else {
                _this.searchItem = _this.searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();
                _this.getDataPaged(1, _this.searchItem, _this.searchColumn);
            }
        },
        // [ 時間格式轉換 ]
        formatTime(timeStr) {
            const date = new Date(timeStr);
            if (isNaN(date.getTime())) {
                // 時間格式不正確，返回空字串
                return '';
            }
            const year = date.getFullYear();
            const month = ('0' + (date.getMonth() + 1)).slice(-2);
            const day = ('0' + date.getDate()).slice(-2);
            const hours = ('0' + date.getHours()).slice(-2);
            const minutes = ('0' + date.getMinutes()).slice(-2);
            const seconds = ('0' + date.getSeconds()).slice(-2);
            return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
        },
        // [ 獲取連結的小物 (測試) ]
        linkGetPage: async function () {
            let _this = this;
            var getUrlString = location.href;
            var url = new URL(getUrlString);
            //console.log(url.searchParams.get('page'));
            pageNow = url.searchParams.get('page');
            searchItem = url.searchParams.get('search');
            if (pageNow === null) {
                pageNow = 1;
            }
            _this.getDataPaged(pageNow);
        },
        // [ 切換頁面Content內容 (C,R,U,D) & 獲取目前編輯資料]
        content(Num, MINo) {
            let _this = this;
            _this.contentNow = Num;
            // ↓取得目前編輯資料
            if (MINo != null) {
                axios.get(`/api/SystemItem/GetManageItem_ByMINo/${MINo}`).then(
                    response => {
                        let _this = this;
                        _this.editMINo = MINo;
                        _this.editData = response.data;
                    }
                ).catch(
                    error => {
                        console.log(error);
                    }
                );
            }
        },
        // [ 頁碼產生 ]
        generatePagination() {
            let _this = this;
            let pages = [];
            let startPage, endPage;
            let totalPages = this.pageTotal.TotalPages;
            _this.pageNow = this.pageTotal.CurrentPage;

            if (totalPages <= 5) {
                startPage = 1;
                endPage = totalPages;
            } else {
                if (_this.pageNow <= 3) {
                    startPage = 1;
                    endPage = 5;
                } else if (_this.pageNow + 1 >= totalPages) {
                    startPage = totalPages - 4;
                    endPage = totalPages;
                } else {
                    startPage = _this.pageNow - 2;
                    endPage = _this.pageNow + 2;
                }
            }
            for (let i = startPage; i <= endPage; i++) {
                pages.push(i);
            }
            return pages;
        },
        // ----------------------------------------------------
        // 【 新增 】
        create() {
            let _this = this;
            var request = {};
            request.MCNo = _this.addData.mCNo;
            request.ItemName = _this.addData.itemName;
            request.MIAction = _this.addData.mIAction;
            request.PowerView = _this.addData.powerView;
            request.PowerAdd = _this.addData.powerAdd;
            request.PowerDel = _this.addData.powerDel;
            request.PowerUpdate = _this.addData.powerUpdate;
            request.PowerGrant = _this.addData.powerGrant;
            request.Power1 = _this.addData.power1;
            request.Power2 = _this.addData.power2;
            request.Order = _this.addData.order;
            request.State = 0;
            request.MSNo = 1;

            axios.post(`/api/SystemItem/AddManageItem`, request)
                .then(response => {
                    let MCXtrol = _this.getMCXtrolByMCNo(_this.addData.mCNo);
                    // 去產生Cshtml
                    _this.CreateCshtmlFile(MCXtrol, _this.addData.mIAction);
                    // 在Controller產生Action
                    _this.AddViewAction(MCXtrol, _this.addData.mIAction);
                    alert("新增成功");
                    window.location = `/System/Item`;
                })
                .catch(error => {
                    alert('您有未完成的內容或填寫錯誤');
                });
        },
        // ----------------------------------------------------
        // 【 修改 】
        update() {
            let _this = this;
            var request = {};
            request.MCNo = _this.editData[0].MCNo;
            request.ItemName = _this.editData[0].ItemName;
            request.MIAction = _this.editData[0].MIAction;
            request.PowerView = _this.editData[0].PowerView;
            request.PowerAdd = _this.editData[0].PowerAdd;
            request.PowerDel = _this.editData[0].PowerDel;
            request.PowerUpdate = _this.editData[0].PowerUpdate;
            request.PowerGrant = _this.editData[0].PowerGrant;
            request.Power1 = _this.editData[0].Power1;
            request.Power2 = _this.editData[0].Power2;
            request.MSNo = _this.editData[0].MSNo;
            request.Order = _this.editData[0].Order;

            axios.patch(`/api/SystemItem/UpdateManageItem/${_this.editMINo}`, request)
                .then(response => {
                    alert("修改成功");
                    window.location = `/System/Item`;
                })
                .catch(error => alert(error));
        },

        // ----------------------------------------------------
        // 【 刪除 】
        deleteSelected() {
            if (this.selectAll) {
                this.checkAll();
            }
            // 取得被勾選的項目
            const selectedItems = this.selectedItems;

            // 如果沒有項目被勾選，結束函數
            if (selectedItems.length === 0) {
                alert('您沒有選擇要刪除的資料')
                return;
            }

            // 將勾選的項目編號做成一個字串
            const checkedParams = selectedItems.map(item => `MINo=${item}`).join('&');

            // 呼叫 deleteRenderingData 方法
            this.deleteData(checkedParams)
                .then(() => {
                    // 清空 selectedItems 陣列
                    this.selectedItems = [];
                })
                .catch((error) => {
                    // 刪除失敗，顯示錯誤訊息
                    alert('刪除失敗');
                });
        },
        /* 批次刪除資料 */
        deleteData(checkedParams) {
            //console.log(this.selectedItems);
            if (confirm("確定要刪除所選擇的資料嗎？")) {
                axios.delete(`/api/SystemItem/DeleteItemClassBatch/batch?${checkedParams}`)
                    .then(response => {
                        this.getDataPaged(this.pageNow);
                        setTimeout(function () {
                            alert('刪除成功！');
                        }, 180);
                    })
                    .catch(error => {
                        console.log(error);
                    });
            }
        },
        /* 全選 */
        checkAll() {
            let delCodes = document.getElementsByName("del_code");
            let checkAll = document.getElementById("checkAll");
            for (let i = 0; i < delCodes.length; i++) {
                delCodes[i].checked = checkAll.checked;
            }
            // 更新選取項目的陣列
            this.selectedItems = checkAll.checked ? this.RenderingData.map(item => item.MINo) : [];
        },
        // ----------------------------------------------------
        // 【 權限 】
        getPermissionValue(permiss, minNo = '2') {
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetStaffPermissions/${STNo}`)
                        .then(response => {
                            const permissions = response.data;
                            const permission = permissions.find(p => p.MINo === parseInt(minNo, 10));
                            if (permission) {
                                this.permissionValue = permission
                                this.$data[permiss] = this.permissionValue[permiss];
                            } else {
                                this.permissionValue = null;
                                this.$data[permiss] = this.permissionValue;
                            }
                        })
                        .catch(error => {
                            console.error('Error:', error);
                        });
                })
                .catch(error => {
                    console.log(error);
                });
        },
    },
}).mount("#ItemVue");

// 【 Website/Info.cshtml 】
const HotelInfoVue = Vue.createApp({
    head() {
        return {
            script: [
                {
                    src: '~/lib/ckeditor/ckeditor.js',
                    type: 'text/javascript'
                }
            ]
        };
    },
    data() {
        return {
            // 主要顯示資料存放區 //
            RenderingData: null,
            // 現在頁面 & 導覽列  //
            contentNow: 1,
            breadcrumb: {
                1: '',
                2: '-新增',
                3: '-修改',
            },
            //   新增 & 編輯   //
            addData: {},
            editData: {},
            editMINo: null,
            //  放在  AXIOS 傳遞 //
            pageSize: 10, //一頁幾筆資料
            pageNow: 1,
            Sort: 'ASC',
            Column: '%5BOrder%5D',
            /* ------------------ */
            pageTotal: null,
            selectedItems: [],
            /* ------------------ */
            SysClassData: null,
            /* ------------------ */
            PV: 0,
            PA: 0,
            PD: 0,
            PU: 0,
            PG: 0,
            P1: 0,
            P2: 0,
        };
    },
    created() {
        // 設置預設值: 新增
        this.addData = {
            mCNo: 0,
            powerView: 0,
            powerAdd: 0,
            powerDel: 0,
            powerUpdate: 0,
            powerGrant: 0,
            power1: '',
            power2: '',
            order: '0'
        };
    },
    mounted() {
        let _this = this;
        _this.content();
        //_this.getPermissionValue('PV');
        //_this.getPermissionValue('PA');
        //_this.getPermissionValue('PD');
        //_this.getPermissionValue('PU');
        //_this.getPermissionValue('PG');
        //_this.getPermissionValue('P1');
        //_this.getPermissionValue('P2');  
    },
    components: {
        'first-component': {
            template: `
        <textarea id="F_S_H_TrafficAdvert" placeholder="" name='F_S_H_TrafficAdvert' class="form-control" rows="10"></textarea>
              <script>
        try {
          CKEDITOR.replace('F_S_H_TrafficAdvert', {
            filebrowserUploadUrl: "CKEditorUpload.aspx",
            filebrowserImageBrowseUrl: "CKEditorBrowse.aspx",
            extraPlugins: "colorbutton,colordialog,youtube",
            image_previewText: " "
          });
        } catch (e) {}
      </script>
      `
        }
    },
    methods: {
        content(Num) {
            let _this = this;
            _this.contentNow = Num;
            // ↓取得目前編輯資料
            axios.get(`/api/HotelDetail/GetHotelInfo`).then(
                response => {
                    let _this = this;
                    _this.editData = response.data;
                }
            ).catch(
                error => {
                    console.log(error);
                }
            );
        }, 
        // [ 時間格式轉換 ]
        formatTime(timeStr) {
            const date = new Date(timeStr);
            if (isNaN(date.getTime())) {
                // 時間格式不正確，返回空字串
                return '';
            }
            const year = date.getFullYear();
            const month = ('0' + (date.getMonth() + 1)).slice(-2);
            const day = ('0' + date.getDate()).slice(-2);
            const hours = ('0' + date.getHours()).slice(-2);
            const minutes = ('0' + date.getMinutes()).slice(-2);
            const seconds = ('0' + date.getSeconds()).slice(-2);
            return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
        },
        // [ 獲取連結的小物 (測試) ]
        linkGetPage: async function () {
            let _this = this;
            var getUrlString = location.href;
            var url = new URL(getUrlString);
            //console.log(url.searchParams.get('page'));
            pageNow = url.searchParams.get('page');
            searchItem = url.searchParams.get('search');
            if (pageNow === null) {
                pageNow = 1;
            }
            _this.getDataPaged(pageNow);
        },
        // ----------------------------------------------------
        // 【 修改 】
        update() {
            let _this = this;
            let request = {
                HANo: _this.editData.HANo,
                HTNo: _this.editData.HTNo,
                HName: _this.editData.HName,
                HTel: _this.editData.HTel,
                HEmail: _this.editData.HEmail,
                HAddr: _this.editData.HAddr,
                Citizen: _this.editData.Citizen,
                HSearchWD: _this.editData.HSearchWD,
                HDescription: _this.editData.HDescription,
                HLongitude: _this.editData.HLongitude,
                HLatitude: _this.editData.HLatitude,
                HLineID: _this.editData.HLineID,
                HLineAt: _this.editData.HLineAt,
                HFB: _this.editData.HFB,
                TWStayNo: _this.editData.TWStayNo,
                CheckinTime: _this.editData.CheckinTime,
                ReserveTime: _this.editData.ReserveTime,
                CheckoutTime: _this.editData.CheckoutTime,
                FormalOpen: _this.editData.FormalOpen,
                Decoration: _this.editData.Decoration,
                Deeds: _this.editData.Deeds,
                RoomAdvert: _this.editData.RoomAdvert,
                HBrief: _this.editData.HBrief,
                CloseRemind: _this.editData.CloseRemind,
                Note: _this.editData.Note,
                TrafficAdvert: _this.editData.TrafficAdvert,
                MTime: _this.editData.MTime,
                MSNo: _this.editData.MSNo
            };
            axios.patch(`/api/HotelDetail/UpdateEHotelInfo/4`, request)
                .then(response => {
                    alert("修改成功");
                    window.location = `/System/Item`;
                })
                .catch(error => alert(error));
        },
        // ----------------------------------------------------
        // 【 權限 】
        getPermissionValue(permiss, minNo = '2') {
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetStaffPermissions/${STNo}`)
                        .then(response => {
                            const permissions = response.data;
                            const permission = permissions.find(p => p.MINo === parseInt(minNo, 10));
                            if (permission) {
                                this.permissionValue = permission
                                this.$data[permiss] = this.permissionValue[permiss];
                            } else {
                                this.permissionValue = null;
                                this.$data[permiss] = this.permissionValue;
                            }
                        })
                        .catch(error => {
                            console.error('Error:', error);
                        });
                })
                .catch(error => {
                    console.log(error);
                });
        },
    },
}).mount("#HotelInfoVue");

// 【 System/Staff.cshtml 】
const StaffVue = Vue.createApp({
    data() {
        return {
            // 主要顯示資料存放區 //
            RenderingData: null,
            // 現在頁面 & 導覽列  //
            contentNow: 1,
            breadcrumb: {
                1: '',
                2: '-新增',
                3: '-修改',
                4: '-修改密碼',
            },
            //   新增 & 編輯   //
            addData: {},
            editData: {},
            editPower: [],
            selectedPower: [], // 新增的陣列
            cancelledPower: [],
            editMINo: null,
            AllPower: false,
            STNo: 1,
            //  放在  AXIOS 傳遞 //
            pageSize: 10, //一頁幾筆資料
            pageNow: 1,
            Sort: 'ASC',
            Column: 'STNo',
            /* ------------------ */
            pageTotal: null,
            selectedItems: [],
            /* ------------------ */
            ManageItem: null,
            SysClassData: null,
            StaffPower: null,
            /* ------------------ */
            PV: 0,
            PA: 0,
            PD: 0,
            PU: 0,
            PG: 0,
            P1: 0,
            P2: 0,
            newPasswd: '',
            newPasswd2:'',
        };
    },
    mounted() {
        let _this = this;
        _this.getDataPaged(_this.pageNow);
        _this.getSysClassData();
        _this.linkGetPage();
        _this.getStaffPower();
        _this.getPermissionValue('PV');
        _this.getPermissionValue('PA');
        _this.getPermissionValue('PD');
        _this.getPermissionValue('PU');
        _this.getPermissionValue('PG');
        _this.getPermissionValue('P1');
        _this.getPermissionValue('P2');
    },
    methods: {
        // << 搜尋 & 資料渲染(分頁) >>
        getDataPaged(pageNow, searchItem = '', searchColumn = 'STNo') {
            // 中文、英文以外空格、特殊符號都刪除
            searchItem = searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();

            let apiUrl = `/api/SystemMember/GetPaged?`;
            if (searchItem === '') {
                apiUrl += `page=${pageNow}&pageSize=${this.pageSize}&selectFrom=Staff&orderBy=${this.Column}%20${this.Sort}`;
            } else {
                apiUrl += `search=${searchItem}&column=${searchColumn}&page=${pageNow}&pageSize=${this.pageSize}&selectFrom=Staff&orderBy=${this.Column}%20${this.Sort}`;
            }

            axios.get(apiUrl)
                .then(response => {
                    this.pageTotal = response.data.Paging;
                    this.RenderingData = response.data.Data;
                    this.searchItem = searchItem;
                    this.searchColumn = searchColumn;
                    this.pageNow = pageNow;
                })
                .catch(error => {
                    console.log(error);
                });
        },
        // < 獲得控制器資料 >
        getSysClassData() {
            let _this = this;
            axios.get(`/api/SystemClass/GetManageClass/`).then(
                response => {
                    _this.SysClassData = response.data;
                }
            );
        },
        // < 由 MCNo 獲取 Controller 名稱 >
        getMCNameByMCNo(MCNo) {
            let foundMC = this.SysClassData.find(item => item.MCNo === MCNo);
            if (foundMC) {
                return foundMC.MCName;
            } else {
                return null;
            }
        },
        getManageItem() {
            let _this = this;
            axios.get(`/api/SystemItem/GetManageItem`).then(
                response => {
                    _this.ManageItem = response.data.filter(item => item.MCNo >= 1).sort((a, b) => a.MCNo - b.MCNo);
                }
            );
        },
        getStaffPower() {
            let _this = this;
            axios.get(`/api/SystemMember/GetStaffPower/${this.STNo}`).then(
                response => {
                    _this.StaffPower = response.data;
                }
            );
        },
        // << 獲得權限設置 >>
        getStaffPowerByEpower(MINo, Epower = '') {
            let foundMC = this.StaffPower.find(item => item.MINo === MINo);
            if (foundMC) {
                return foundMC[Epower];
            } else {
                return null;
            }
        },
        // << 切換分頁 >>
        goToPage(page) {
            if (this.searchItem === '') {
                this.getDataPaged(page);
            } else {
                this.getDataPaged(page, this.searchItem, this.searchColumn);
            }
            // 切換分頁時取消全選
            let checkAll = document.getElementById("checkAll");
            checkAll.checked = false;
            this.checkAll(); // 調用checkAll方法，將複選框狀態更新到selectedItems陣列。
        },
        // --------------------------------------------------------------------------------
        // [ 資料排序 ]
        sortBy(column) {
            let _this = this;
            if (_this.Sort === 'DESC') {
                _this.Sort = 'ASC';
            } else {
                _this.Sort = 'DESC';
            }
            _this.Column = column;
            // 以下是在同時確認目前狀態，是搜尋模式還是全部列表
            if (this.searchItem === '') {
                _this.getDataPaged(_this.pageNow);
            } else {
                _this.searchItem = _this.searchItem.replace(/[^\w\s\u4e00-\u9fa5]/gi, '').replace(/\s+/g, ' ').trim();
                _this.getDataPaged(1, _this.searchItem, _this.searchColumn);
            }
        },
        // [ 時間格式轉換 ]
        formatTime(timeStr) {
            const date = new Date(timeStr);
            if (isNaN(date.getTime())) {
                // 時間格式不正確，返回空字串
                return '';
            }
            const year = date.getFullYear();
            const month = ('0' + (date.getMonth() + 1)).slice(-2);
            const day = ('0' + date.getDate()).slice(-2);
            const hours = ('0' + date.getHours()).slice(-2);
            const minutes = ('0' + date.getMinutes()).slice(-2);
            const seconds = ('0' + date.getSeconds()).slice(-2);
            return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
        },
        // [ 獲取連結的小物 (測試) ]
        linkGetPage: async function () {
            let _this = this;
            var getUrlString = location.href;
            var url = new URL(getUrlString);
            //console.log(url.searchParams.get('page'));
            pageNow = url.searchParams.get('page');
            searchItem = url.searchParams.get('search');
            if (pageNow === null) {
                pageNow = 1;
            }
            _this.getDataPaged(pageNow);
        },
        // [ 切換頁面Content內容 (C,R,U,D) & 獲取目前編輯資料]
        content(Num, STNo) {
            let _this = this;
            _this.contentNow = Num;
            // ↓取得目前編輯資料
            if (STNo != null) {
                axios.get(`/api/SystemMember/GetManageMember_BySTNo/${STNo}`).then(
                    response => {
                        let _this = this;
                        _this.STNo = STNo;
                        _this.editData = response.data;
                        _this.getManageItem();
                        _this.getStaffPower()
                        if (_this.editData[0].AllPower == 1) {
                            this.AllPower = true;
                        } else {
                            this.AllPower = false;
                        }
                    }
                ).catch(
                    error => {
                        console.log(error);
                    }
                );
            }
        },
        // [ 頁碼產生 ]
        generatePagination() {
            let _this = this;
            let pages = [];
            let startPage, endPage;
            let totalPages = this.pageTotal.TotalPages;
            _this.pageNow = this.pageTotal.CurrentPage;
            if (totalPages <= 5) {
                startPage = 1;
                endPage = totalPages;
            } else {
                if (_this.pageNow <= 3) {
                    startPage = 1;
                    endPage = 5;
                } else if (_this.pageNow + 1 >= totalPages) {
                    startPage = totalPages - 4;
                    endPage = totalPages;
                } else {
                    startPage = _this.pageNow - 2;
                    endPage = _this.pageNow + 2;
                }
            }
            for (let i = startPage; i <= endPage; i++) {
                pages.push(i);
            }
            return pages;
        },
        // ----------------------------------------------------
        // 【 新增 】
        create() {
            let _this = this;
            var request = {};
            request.MCNo = _this.addData.mCNo;
            request.ItemName = _this.addData.itemName;
            request.MIAction = _this.addData.mIAction;
            request.PowerView = _this.addData.powerView;
            request.PowerAdd = _this.addData.powerAdd;
            request.PowerDel = _this.addData.powerDel;
            request.PowerUpdate = _this.addData.powerUpdate;
            request.PowerGrant = _this.addData.powerGrant;
            request.Power1 = _this.addData.power1;
            request.Power2 = _this.addData.power2;
            request.Order = _this.addData.order;
            request.State = 0;
            request.MSNo = 1;
            //console.log(request);

            axios.post(`/api/SystemItem/AddManageItem`, request)
                .then(response => {
                    let MCXtrol = _this.getMCXtrolByMCNo(_this.addData.mCNo);
                    // 去產生Cshtml
                    _this.CreateCshtmlFile(MCXtrol, _this.addData.mIAction);
                    // 在Controller產生Action
                    _this.AddViewAction(MCXtrol, _this.addData.mIAction);
                    alert("新增成功");
                    window.location = `/System/Item`;
                })
                .catch(error => {
                    alert(error);
                });
        },
        // ----------------------------------------------------
        // 【 修改 】 終於用好了 e04
        editLoginPasswd(STNo, pass) {
            if ((pass == this.newPasswd2) & (pass != '')) {
                axios.post(`/api/SystemMember/EditLoginPasswd/${STNo}/${pass}`,)
                    .then(() => {
                        alert("修改成功");
                        window.location = `/System/Staff`;
                    })
                    .catch(error => {
                    //    console.log(error);
                    });
            } else if (pass != this.newPasswd2) {
                alert("請確定密碼一致");
            } else {
                alert("請輸入新密碼");
            }
        },
        update() {
            for (let i = 0; i < this.selectedPower.length; i++) {
                const { MINo, ...powers } = this.selectedPower[i];
                const request = {};
                if (powers.PV) {
                    request.PV = 1;
                }
                if (powers.PA) {
                    request.PA = 1;
                }
                if (powers.PD) {
                    request.PD = 1;
                }
                if (powers.PU) {
                    request.PU = 1;
                }

                if (powers.PG) {
                    request.PG = 1;
                }

                if (powers.P1) {
                    request.P1 = 1;
                }
                if (powers.P2) {
                    request.P2 = 1;
                }

                axios.get(`/api/SystemMember/GetOrUpdateStaffPower/${this.STNo}/${MINo}`, {
                    params: request
                });
            }

            for (let i = 0; i < this.cancelledPower.length; i++) {
                const { MINo, ...powers } = this.cancelledPower[i];
                const request = {};
                if (powers.PV !== undefined) {
                    request.PV = 0;
                }
                if (powers.PA !== undefined) {
                    request.PA = 0;
                }
                if (powers.PD !== undefined) {
                    request.PD = 0;
                }
                if (powers.PU !== undefined) {
                    request.PU = 0;
                }

                if (powers.PG !== undefined) {
                    request.PG = 0;
                }

                if (powers.P1 !== undefined) {
                    request.P1 = 0;
                }
                if (powers.P2 !== undefined) {
                    request.P2 = 0;
                }

                axios.get(`/api/SystemMember/GetOrUpdateStaffPower/${this.STNo}/${MINo}`, {
                    params: request
                });
            }

            alert("修改成功");
            window.location = `/System/Staff`;
        },
        // 獲得checkbox勾選物
        updateSelectedPower(key, checked) {
            const [MINo, Epower] = key.split('_');

            // 獲取具有相同 MINo 的複選框的狀態
            const powerState = {};
            const checkboxes = document.querySelectorAll(`input[name^="${MINo}_"]`);
            checkboxes.forEach(checkbox => {
                const [_, power] = checkbox.name.split('_');
                powerState[power] = checkbox.checked ? 1 : 0;
            });

            if (checked) {
                // 添加到 selectedPower 數組
                const selectedIndex = this.selectedPower.findIndex(item => item.MINo === MINo);
                if (selectedIndex > -1) {
                    this.selectedPower[selectedIndex][Epower] = 1;
                } else {
                    this.selectedPower.push({
                        MINo: MINo,
                        [Epower]: 1,
                        ...powerState
                    });
                }

                // 如果同時存在於 cancelledPower 數組中，則移除
                const cancelledIndex = this.cancelledPower.findIndex(item => item.MINo === MINo);
                if (cancelledIndex > -1) {
                    delete this.cancelledPower[cancelledIndex][Epower];
                    if (Object.keys(this.cancelledPower[cancelledIndex]).length === 1) {
                        this.cancelledPower.splice(cancelledIndex, 1);
                    }
                }
            } else {
                // 移除從 selectedPower 數組中
                const selectedIndex = this.selectedPower.findIndex(item => item.MINo === MINo);
                if (selectedIndex > -1) {
                    delete this.selectedPower[selectedIndex][Epower];
                    if (Object.keys(this.selectedPower[selectedIndex]).length === 1) {
                        this.selectedPower.splice(selectedIndex, 1);
                    }
                }

                // 添加到 cancelledPower 數組
                const cancelledIndex = this.cancelledPower.findIndex(item => item.MINo === MINo);
                if (cancelledIndex > -1) {
                    this.cancelledPower[cancelledIndex][Epower] = 0;
                } else {
                    this.cancelledPower.push({
                        MINo: MINo,
                        [Epower]: 0,
                        ...powerState
                    });
                }
            }
        },
        // ----------------------------------------------------
        // 【 刪除 】
        deleteSelected() {
            if (this.selectAll) {
                this.checkAll();
            }
            // 取得被勾選的項目
            const selectedItems = this.selectedItems;

            // 如果沒有項目被勾選，結束函數
            if (selectedItems.length === 0) {
                alert('您沒有選擇要刪除的資料')
                return;
            }

            // 將勾選的項目編號做成一個字串
            const checkedParams = selectedItems.map(item => `STNo=${item}`).join('&');

            // 呼叫 deleteRenderingData 方法
            this.deleteData(checkedParams)
                .then(response => {
                    // 清空 selectedItems 陣列
                    this.selectedItems = [];
                })
                .catch(error => {
                    // 刪除失敗，顯示錯誤訊息
                    console.error(error);
                });
        },
        /* 批次刪除資料 */
        deleteData(checkedParams) {
            if (confirm("確定要刪除所選擇的資料嗎？")) {
                axios.delete(`/api/SystemMember/DeleteStaffBatch/batch?${checkedParams}`)
                    .then(response => {
                        this.getDataPaged(this.pageNow);
                        showAlert('刪除成功！', 180);
                    })
                    .catch(error => {
                        console.log(error);
                    });
            }
        },
        /* 全選 */
        checkAll() {
            let delCodes = document.getElementsByName("del_code");
            let checkAll = document.getElementById("checkAll");
            for (let i = 0; i < delCodes.length; i++) {
                delCodes[i].checked = checkAll.checked;
            }
            // 更新選取項目的陣列
            this.selectedItems = checkAll.checked ? this.RenderingData.map(item => item.STNo) : [];
        },
        // ----------------------------------------------------
        // 【 權限 】
        getPermissionValue(permiss, minNo = 3) {
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetStaffPermissions/${STNo}`)
                        .then(response => {
                            const permissions = response.data;
                            const permission = permissions.find(p => p.MINo === parseInt(minNo, 10));
                            if (permission) {
                                this.permissionValue = permission
                                this.$data[permiss] = this.permissionValue[permiss];
                            } else {
                                this.permissionValue = null;
                                this.$data[permiss] = this.permissionValue;
                            }
                        })
                        .catch(error => {
                            console.error('Error:', error);
                        });
                })
                .catch(error => {
                    console.log(error);
                });
        },
        getSTNameByToken() {
            const token = localStorage.getItem('Token');
            axios.get(`/api/AccountLogin/GetUserInfo`, {
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            })
                .then(response => {
                    const STNo = response.data.STNo;
                    axios.get(`/api/AccountLogin/GetSTNameBySTNo/${STNo}`)
                        .then(response => {
                            this.STName = response.data;
                        })
                        .catch(error => {
                            this.STName = 'Unknown'
                            //    console.log(error);
                        });
                })
                .catch(error => {
                    this.STName = 'Unknown#'
                    //    console.log(error);
                });
        },
    },
}).mount("#StaffVue");

// 【 Food/Index.cshtml 】 這隻是起初的測試，還沒完成的
const OHtelVue = Vue.createApp({
    data() {
        return {
            EHotelFoodData: null,
            pageSize: 4,
            pageNow: 1,
            pageTotal: null,
        };
    },
    mounted() {
        let _this = this;
        _this.getEHotelFoodData(_this.pageNow);
    },
    methods: {
        getEHotelFoodData(pageNow) {
            axios.get(`/api/EHotelFood/GetEHotel_Food_Paged?page=${pageNow}&pageSize=${this.pageSize}&selectFrom=EHotel_Food&orderBy=FoodNo%20ASC`).then(
                response => {
                    let _this = this;
                    _this.pageTotal = response.data.Paging;
                    _this.EHotelFoodData = response.data.Data.map(item => {
                        // 將 Categories 欄位的內容轉換成中文
                        switch (item.Categories) {
                            case "food":
                                item.Categories = "主餐";
                                break;
                            case "snack":
                                item.Categories = "甜點";
                                break;
                            case "beverage":
                                item.Categories = "飲料";
                                break;
                            default:
                                item.Categories = "其他";
                                break;
                        }
                        // 將 FoodIsSell 欄位的值轉換成中文狀態
                        switch (item.FoodIsSell) {
                            case 1:
                                item.FoodIsSell = "已發布";
                                break;
                            case 0:
                                item.FoodIsSell = "停止";
                                break;
                            default:
                                item.FoodIsSell = "未知狀態";
                                break;
                        }
                        return item;
                    });
                }
            ).catch(
                error => {
                    console.log(error);
                }
            );
        },
    },
}).mount("#OHtelVue");
// ------------------------------------------------------------ VUE End ------------------------------------------------------------