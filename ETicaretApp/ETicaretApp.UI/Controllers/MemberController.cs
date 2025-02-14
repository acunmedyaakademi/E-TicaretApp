﻿using ETicaretApp.BLL;
using ETicaretApp.DAL.EntityFramework;
using ETicaretApp.Entities;
using ETicaretApp.UI.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NETCore.Encrypt.Extensions;
using System.Security.Claims;
using SendGrid.Helpers.Mail;
using SendGrid;
using ETicaretApp.UI.Helpers;

namespace ETicaretApp.UI.Controllers
{
    public class MemberController : Controller
    {
        MemberManager memberManager = new MemberManager(new EfMemberRepository());
        private readonly IConfiguration configuration;


        public MemberController(IConfiguration configuration)
        {
            this.configuration = configuration;

        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashedPassword;
                EncryptHelper.EncryptPassword(model.Password, out hashedPassword);

                var user = memberManager.ListAll().FirstOrDefault(x => x.Email.ToLower() == model.Email.ToLower() && x.Password.ToLower() == hashedPassword.ToLower());

                if (user != null && user.State)
                {
                    List<Claim> claims = new List<Claim>
                    {
                     new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                     new Claim(ClaimTypes.Email,user.Email)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Kullanıcı bilgileri hatalı");
                        return View(model);

                    }
                    else
                    {
                        ModelState.AddModelError("", "Kullanıcı aktif değil");
                        return View(model);

                    }
                }


            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            try
            {
                if (!model.Terms)
                    ModelState.AddModelError(nameof(model.Terms), "Şartları Kabul ediniz.");

                if (ModelState.IsValid)
                {
                    if (memberManager.Query().Any(x => x.Email.ToLower() == model.Email.ToLower()))
                    {
                        ModelState.AddModelError(nameof(model.Email), "Email kullanılıyor");
                        return View(model);

                    }


                    string hashedPassword;
                    EncryptHelper.EncryptPassword(model.Password, out hashedPassword);



                    Member member = new Member()
                    {
                        Email = model.Email,
                        Password = hashedPassword,
                        RegisterDate = DateTime.Now,
                        State = true
                    };
                    memberManager.Create(member);
                }
                else
                {
                    return View(model);
                }

            }
            catch (Exception ex)
            {

                ModelState.AddModelError("", ex.Message);
            }

            return RedirectToAction(nameof(Login));
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var member = memberManager.ListAll().Where(x => x.Email == model.Email).FirstOrDefault();

                    if (member == null)
                    {
                        ModelState.AddModelError("", "eposta bulunamadı");
                        return View(model);
                    }


                    // SendGrid API anahtarınızı burada belirtin
                    string sendGridApiKey = configuration.GetValue<string>("AppSettings:MailApi");

                    // SendGrid istemcisini oluşturun
                    var client = new SendGridClient(sendGridApiKey);

                    // E-posta gövdesini oluşturun
                    var from = new EmailAddress(configuration.GetValue<string>("AppSettings:SenderMail"), "ETicaretApp Şifre Sıfırlama");
                    var to = new EmailAddress(model.Email);
                    var subject = "Şifrenizi Sıfırlayın";
                    var plainTextContent = $"Şifrenizi sıfırlamak için aşağıdaki bağlantıyı kullanın: https://localhost:7176/Member/RecoverPassword/{member.Id}";
                    var htmlContent = $"<p>Şifrenizi sıfırlamak için <a href=\"https://localhost:7176/Member/RecoverPassword/{member.Id}\">buraya</a> tıklayın.</p>";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                    // E-postayı gönderin
                    var response = await client.SendEmailAsync(msg);

                    // İşlem sonucunu kontrol edin
                    if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    {
                        return RedirectToAction(nameof(RecoverPassword));
                        // E-posta başarıyla gönderildi
                    }
                    else
                    {
                        // E-posta gönderme başarısız oldu
                        // Hata ile ilgili işlemleri burada yapabilirsiniz
                        return RedirectToAction(nameof(Login));
                    }
                }
                else
                    return View(model);

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);

            }
            return View(model);
        }


        public IActionResult RecoverPassword(Guid id, RecoverPasswordViewModel model)
        {
            try

            {
                if (ModelState.IsValid)
                {
                    var member = memberManager.GetByGuid(id);
                    if (member == null)
                    {
                        return RedirectToAction(nameof(Login));
                    }
                    //string md5Salt = configuration.GetValue<string>("AppSettings:MD5Salt");
                    //string saltedPassword = model.Password + md5Salt;
                    //string hashedPassword = saltedPassword.MD5();
                    string hashedPassword;
                    EncryptHelper.EncryptPassword(model.Password, out hashedPassword);

                    memberManager.Update(new Member
                    {
                        Id = member.Id,
                        Password = hashedPassword,
                        Email = member.Email,
                        FullName = member.FullName,
                        RegisterDate = member.RegisterDate,
                        State = member.State
                    });
                    return RedirectToAction(nameof(Login));
                }
                else
                    return View(model);

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View();
        }
    }
}
