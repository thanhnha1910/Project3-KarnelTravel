import React from 'react'
import ReactDOM from 'react-dom/client'
import {BrowserRouter, Route, Routes} from 'react-router-dom'
import './index.css'
import {ToastContainer} from "react-toastify";
import Login from "./pages/auth/Login.jsx";
import Register from "./pages/auth/Register.jsx";
import VerifyEmail from "./pages/auth/VerifyEmail.jsx";
import ResetPassWord from "./pages/auth/ResetPassWord.jsx";
import RequestResetPassword from "./pages/auth/RequestResetPassword.jsx";
import HomeLayout from "./layouts/home-layout.jsx";
import Home from "./pages/home/home.jsx";
import Search from "./pages/home/search.jsx";
import About from "./pages/home/about.jsx";
import Contact from "./pages/home/contact.jsx";

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <BrowserRouter>
            <ToastContainer/>
            <Routes>
                <Route element={<HomeLayout/>}>
                    <Route index element={<Home/>}/>
                    <Route path="search" element={<Search/>}/>
                    <Route path="about" element={<About/>}/>
                    <Route path="contact" element={<Contact/>}/>
                </Route>

                <Route path="login" element={<Login/>}/>
                <Route path="register" element={<Register/>}/>
                <Route path="verify-email" element={<VerifyEmail/>}/>
                <Route path="reset-password" element={<ResetPassWord/>}/>
                <Route path="request-reset-password" element={<RequestResetPassword/>}/>
            </Routes>
        </BrowserRouter>
    </React.StrictMode>
)