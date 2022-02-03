namespace GlobalPollenProject.Identity.Contract

type SecurityToken = {
    auth_token: string
}

type Login = {
    Username: string
    Password: string
}

type ConfirmCode = {
    Username: string
    Code: string
}