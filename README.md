# BodWebAPI 🚀✨

Hi~ 歡迎來到 **BodWebAPI**！
這裡是後端 API 文件，若有任何問題歡迎聯絡我們～🙌

---

## AuthController 認證授權相關

### /api/Auth/register 會員註冊 ✨
- `POST`
- 請求範例：
```json
{
    "userName": "string",
    "password": "string",
    "email": "string",
    "phoneNumber": "string",
    "birthday": "Date Time"
}
```
- 回應訊息：
```json
{
    "message": "會員資料新增完成"
}
```

### /api/Auth/login 會員登入 🔑
- `POST`
- 請求範例：
```json
{
    "account": "string (可輸入Email、電話或使用者名稱)",
    "password": "string"
}
```
- 回應訊息：
```json
{
    "token": "jwt token..."
}
```

---

## TestController 測試用途

### /api/Test/public 公開API🌍
- `GET`
- 不需權限
- 回應：This is public

### /api/Test/private 私有API🔒
- `GET`
- 需JWT驗證
- 回應：
```json
{
    "UserId": "string",
    "UserName": "string",
    "UserEmail": "string"
}
```

### /api/Test/secret Admin專屬🛡️
- `GET`
- 需角色為Admin
- 回應：
```json
{
    "message": "你是Admin！"
}
```

---

更多功能開發中，敬請期待！🚧🌟
