using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyCompany.Domain;
using MyCompany.Domain.Repositories.Abstract;
using MyCompany.Domain.Repositories.EntityFramework;
using MyCompany.Service;

namespace MyCompany
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) => Configuration = configuration;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //подставляем конфиг из апсеттинг json
            Configuration.Bind("Project",new Config());

                //подключаем нужный функционал приложений в качестве сервисов
                services.AddTransient<ITextFieldRepository,EFTextFieldsRepository>();
                services.AddTransient<IServiceItemsRepository, EFServiceItemsRepository>();
                services.AddTransient<DataManager>();

                //подключаем контекст БД
                services.AddDbContext<AppDbContext>(x => x.UseSqlServer(Config.ConnectionString));

                // настраиваем identity систему
                services.AddIdentity<IdentityUser, IdentityRole>(
                //    opts =>
                //{
                //    opts.User.RequireUniqueEmail = true;
                //    opts.Password.RequiredLength = 6;
                //    opts.Password.RequireNonAlphanumeric = false;
                //    opts.Password.RequireLowercase = false;
                //    opts.Password.RequireUppercase = false;
                //    opts.Password.RequireDigit = false;
                //}
                InitIdentityOptions
                    ).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

                //настраиваем authentication cookies
                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.Name = "myCompanyAuth";
                    options.Cookie.HttpOnly = true;
                    options.LoginPath = "/account/login";
                    options.AccessDeniedPath = "/account/accessdenied";
                    options.SlidingExpiration = true;
                });

            //настраиваем политику авторизации для админ ариа
            services.AddAuthorization(x =>
            {
                x.AddPolicy("AdminArea", policy =>
                {
                    policy.RequireRole("admin");

                });
            });

            //добавляем поддержку контроллеров и представлений MVC
            services.AddControllersWithViews(x =>
                {
                    x.Conventions.Add(new AdminAreaAuthorization("Admin","AdminArea"));
                })
                //Выставляем совместимость с Asp.net
                .SetCompatibilityVersion(CompatibilityVersion.Latest).AddSessionStateTempDataProvider();
        }

        public void InitIdentityOptions(IdentityOptions opts)
        {
            //{
            opts.User.RequireUniqueEmail = true;
            opts.Password.RequiredLength = 6;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireLowercase = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireDigit = false;
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            //подключаем систему маршрутизации
            app.UseRouting();
            //подключаем поддержку статических файлов
            app.UseStaticFiles();
            // подключаем аутентификацию и авторизацию
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            //регистрируем нужные нам маршруты
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("admin", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
