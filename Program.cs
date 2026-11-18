using System.Data;
using System.Net;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromHours(4); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddSingleton<Db>();
var app = builder.Build();
app.UseStaticFiles();
app.UseSession();

string Layout(HttpContext c, string title, string body) {
    var user = c.Session.GetString("User");
    var role = c.Session.GetString("Role");
    var nav = user == null ? "" : $"<a href='/dashboard'>Dashboard</a> <a href='/assignments'>Assignments</a> {(role == "Student" ? "<a href='/my-submissions'>My submissions</a>" : "<a href='/staff/submissions'>Student submissions</a>")} <a href='/logout'>Logout ({WebUtility.HtmlEncode(user)})</a>";
    return $$"""
    <!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
    <title>{{WebUtility.HtmlEncode(title)}}</title><link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <style>body{background:#f4f6fb}.navbar{background:#172554}.navbar a{color:white;margin-right:18px;text-decoration:none}.card{border:0;box-shadow:0 5px 20px #17255412;border-radius:14px}.btn-primary{background:#3156d3;border:0}.container{max-width:1100px}</style></head>
    <body><nav class="navbar p-3"><div class="container"><a class="navbar-brand text-white fw-bold" href="/">CAP Portal</a><div>{{nav}}</div></div></nav>
    <main class="container py-4">{{body}}</main></body></html>
    """;
}
string Page(HttpContext c, string title, string body) => Layout(c,title,body);
bool Signed(HttpContext c) => c.Session.GetString("User") != null;
bool Role(HttpContext c, params string[] roles) => roles.Contains(c.Session.GetString("Role") ?? "");
string H(string? s) => WebUtility.HtmlEncode(s ?? "");
app.MapGet("/", (HttpContext c) => Results.Content(Page(c,"College Assignment Portal", Signed(c) ? "<h2>Welcome to College Assignment Portal</h2><p>Use the navigation bar to continue.</p><a class='btn btn-primary' href='/dashboard'>Open dashboard</a>" : "<div class='card p-5 mx-auto' style='max-width:520px'><h2>College Assignment Portal</h2><p class='text-secondary'>Sign in to manage and submit assignments.</p><a class='btn btn-primary' href='/login'>Login</a></div>"),"text/html"));

app.MapGet("/login", (HttpContext c) => Results.Content(Page(c,"Login", """
<div class="card p-4 mx-auto" style="max-width:450px"><h3 class="mb-3">Login</h3>
<form method="post" action="/login"><label class="form-label">Email</label><input class="form-control mb-3" name="email" type="email" required>
<label class="form-label">Password</label><input class="form-control mb-3" name="password" type="password" required>
<button class="btn btn-primary w-100">Sign in</button></form>
<p class="small text-secondary mt-3 mb-0">Demo: admin@college.com / Admin@123</p></div>
"""),"text/html"));
app.MapPost("/login", async (HttpContext c, Db db) => {
    var f = await c.Request.ReadFormAsync();
    var row = await db.One("SELECT UserId, FullName, Role FROM Users WHERE Email=@e AND PasswordHash=@p AND IsActive=1", ("@e",f["email"].ToString()),("@p",f["password"].ToString()));
    if(row == null) return Results.Content(Page(c,"Login failed","<div class='alert alert-danger'>Invalid email or password. <a href='/login'>Try again</a></div>"),"text/html");
    c.Session.SetString("User", Convert.ToString(row["FullName"])!);
    c.Session.SetString("Role", Convert.ToString(row["Role"])!);
    c.Session.SetInt32("UserId", Convert.ToInt32(row["UserId"]));
    return Results.Redirect("/dashboard");
});
app.MapGet("/logout", (HttpContext c) => { c.Session.Clear(); return Results.Redirect("/"); });

app.MapGet("/dashboard", async (HttpContext c, Db db) => {
    if(!Signed(c)) return Results.Redirect("/login");
    var counts = await db.One("SELECT (SELECT COUNT(*) FROM Users WHERE Role='Student') Students,(SELECT COUNT(*) FROM Users WHERE Role='Staff') Staff,(SELECT COUNT(*) FROM Assignments) Assignments,(SELECT COUNT(*) FROM Submissions) Submissions");
    var b = $"""
    <h2 class="mb-4">Dashboard</h2><p class="text-secondary">Hello, {H(c.Session.GetString("User"))} · {H(c.Session.GetString("Role"))}</p>
    <div class="row g-3">
    {Stat("Students",counts?["Students"])}{Stat("Staff",counts?["Staff"])}{Stat("Assignments",counts?["Assignments"])}{Stat("Submissions",counts?["Submissions"])}
    </div><div class="card p-4 mt-4"><h5>Quick actions</h5><a href="/assignments">View assignments</a>
    {(Role(c,"Staff","Admin") ? "<a href='/assignments/new'>Create assignment</a> <a href='/staff/submissions'>View student submissions</a>" : "")}</div>
    """;
    return Results.Content(Page(c,"Dashboard",b),"text/html");
});
string Stat(string label, object? val) => $"<div class='col-md-3'><div class='card p-4'><div class='text-secondary'>{label}</div><div class='display-6 fw-bold'>{val}</div></div></div>";

app.MapGet("/assignments", async (HttpContext c, Db db) => {
    if(!Signed(c)) return Results.Redirect("/login");
    var rows = await db.Many("SELECT a.AssignmentId,a.Title,a.Subject,a.Description,a.DueDate,u.FullName StaffName FROM Assignments a JOIN Users u ON a.StaffId=u.UserId ORDER BY a.DueDate");
    var html = "<div class='d-flex justify-content-between mb-3'><h2>Assignments</h2>"+(Role(c,"Staff","Admin")?"<a class='btn btn-primary' href='/assignments/new'>+ New assignment</a>":"")+"</div><div class='card p-3'><div class='table-responsive'><table class='table align-middle'><thead><tr><th>Title</th><th>Subject</th><th>Due date</th><th>Staff</th><th></th></tr></thead><tbody>";
    foreach(var r in rows) html += $"<tr><td>{H(Convert.ToString(r["Title"]))}</td><td>{H(Convert.ToString(r["Subject"]))}</td><td>{Convert.ToDateTime(r["DueDate"]):dd MMM yyyy}</td><td>{H(Convert.ToString(r["StaffName"]))}</td><td><a href='/assignments/{r["AssignmentId"]}'>Open</a></td></tr>";
    html += "</tbody></table></div></div>";
    return Results.Content(Page(c,"Assignments",html),"text/html");
});
app.MapGet("/assignments/new", (HttpContext c) => {
    if(!Role(c,"Staff","Admin")) return Results.Forbid();
    return Results.Content(Page(c,"Create assignment", """
    <div class="card p-4"><h2>Create assignment</h2><form method="post" action="/assignments/new">
    <label class="form-label">Title</label><input name="title" class="form-control mb-3" required>
    <label class="form-label">Subject</label><input name="subject" class="form-control mb-3" required>
    <label class="form-label">Description / instructions</label><textarea name="description" class="form-control mb-3" rows="5" required></textarea>
    <label class="form-label">Due date</label><input name="dueDate" type="date" class="form-control mb-3" required>
    <button class="btn btn-primary">Publish assignment</button></form></div>
    """),"text/html");
});
app.MapPost("/assignments/new", async (HttpContext c, Db db) => {
    if(!Role(c,"Staff","Admin")) return Results.Forbid();
    var f=await c.Request.ReadFormAsync();
    await db.Exec("INSERT INTO Assignments(Title,Subject,Description,DueDate,StaffId,CreatedAt) VALUES(@t,@s,@d,@due,@id,GETDATE())",
        ("@t",f["title"].ToString()),("@s",f["subject"].ToString()),("@d",f["description"].ToString()),("@due",DateTime.Parse(f["dueDate"]!)),("@id",c.Session.GetInt32("UserId")!.Value));
    return Results.Redirect("/assignments");
});
app.MapGet("/assignments/{id:int}", async (HttpContext c, int id, Db db) => {
    if(!Signed(c)) return Results.Redirect("/login");
    var r=await db.One("SELECT * FROM Assignments WHERE AssignmentId=@id",("@id",id));
    if(r==null) return Results.NotFound();
    var submit = Role(c,"Student") ? $"""
    <hr><h5>Submit your work</h5><form method="post" action="/assignments/{id}/submit" enctype="multipart/form-data">
    <input type="file" name="file" class="form-control mb-3" accept=".pdf,.doc,.docx" required><button class="btn btn-primary">Upload submission</button></form>
    """ : "";
    var body=$"<div class='card p-4'><h2>{H(Convert.ToString(r["Title"]))}</h2><p class='text-secondary'>{H(Convert.ToString(r["Subject"]))} · Due {Convert.ToDateTime(r["DueDate"]):dd MMM yyyy}</p><p style='white-space:pre-wrap'>{H(Convert.ToString(r["Description"]))}</p>{submit}</div>";
    return Results.Content(Page(c,"Assignment",body),"text/html");
});
app.MapPost("/assignments/{id:int}/submit", async (HttpContext c,int id,Db db) => {
    if(!Role(c,"Student")) return Results.Forbid();
    var f=await c.Request.ReadFormAsync(); var file=f.Files.GetFile("file");
    if(file==null || file.Length==0) return Results.BadRequest("Choose a file.");
    var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
    if(!new[]{".pdf",".doc",".docx"}.Contains(ext) || file.Length>10*1024*1024) return Results.BadRequest("Only PDF/DOC/DOCX files up to 10 MB are allowed.");
    var folder=Path.Combine(app.Environment.ContentRootPath,"wwwroot","uploads"); Directory.CreateDirectory(folder);
    var stored=$"{Guid.NewGuid()}{ext}"; using(var stream=File.Create(Path.Combine(folder,stored))) await file.CopyToAsync(stream);
    await db.Exec("INSERT INTO Submissions(AssignmentId,StudentId,FileName,StoredName,SubmittedAt,Status) VALUES(@a,@s,@f,@n,GETDATE(),'Submitted')",
        ("@a",id),("@s",c.Session.GetInt32("UserId")!.Value),("@f",Path.GetFileName(file.FileName)),("@n",stored));
    return Results.Redirect("/my-submissions");
});
app.MapGet("/my-submissions", async (HttpContext c,Db db) => {
    if(!Role(c,"Student")) return Results.Forbid();
    var rows=await db.Many("SELECT s.FileName,s.SubmittedAt,s.Status,a.Title FROM Submissions s JOIN Assignments a ON a.AssignmentId=s.AssignmentId WHERE s.StudentId=@id ORDER BY s.SubmittedAt DESC",("@id",c.Session.GetInt32("UserId")!.Value));
    var html="<h2 class='mb-3'>My submissions</h2><div class='card p-3'><table class='table'><tr><th>Assignment</th><th>File</th><th>Submitted</th><th>Status</th></tr>";
    foreach(var r in rows) html += $"<tr><td>{H(Convert.ToString(r["Title"]))}</td><td>{H(Convert.ToString(r["FileName"]))}</td><td>{Convert.ToDateTime(r["SubmittedAt"]):g}</td><td>{H(Convert.ToString(r["Status"]))}</td></tr>";
    html+="</table></div>";
    return Results.Content(Page(c,"My submissions",html),"text/html");
});
app.MapGet("/staff/submissions", async (HttpContext c,Db db) => {
    if(!Role(c,"Staff","Admin")) return Results.Forbid();
    var rows=await db.Many("SELECT s.SubmissionId,s.FileName,s.StoredName,s.SubmittedAt,s.Status,a.Title,u.FullName Student FROM Submissions s JOIN Assignments a ON a.AssignmentId=s.AssignmentId JOIN Users u ON u.UserId=s.StudentId ORDER BY s.SubmittedAt DESC");
    var html="<h2 class='mb-3'>Student submissions</h2><div class='card p-3'><table class='table'><tr><th>Student</th><th>Assignment</th><th>File</th><th>Submitted</th><th>Status</th></tr>";
    foreach(var r in rows) html += $"<tr><td>{H(Convert.ToString(r["Student"]))}</td><td>{H(Convert.ToString(r["Title"]))}</td><td><a href='/uploads/{Uri.EscapeDataString(Convert.ToString(r["StoredName"])!)}' target='_blank'>{H(Convert.ToString(r["FileName"]))}</a></td><td>{Convert.ToDateTime(r["SubmittedAt"]):g}</td><td>{H(Convert.ToString(r["Status"]))}</td></tr>";
    html+="</table></div>";
    return Results.Content(Page(c,"Submissions",html),"text/html");
});
app.Run();

sealed class Db(IConfiguration config) {
    string Cs => config.GetConnectionString("DefaultConnection")!;
    public async Task<Dictionary<string,object>?> One(string sql, params (string,object)[] p) {
        await using var con=new SqlConnection(Cs); await con.OpenAsync();
        await using var cmd=new SqlCommand(sql,con); foreach(var x in p) cmd.Parameters.AddWithValue(x.Item1,x.Item2??DBNull.Value);
        await using var rd=await cmd.ExecuteReaderAsync(); if(!await rd.ReadAsync()) return null;
        var d=new Dictionary<string,object>(); for(int i=0;i<rd.FieldCount;i++) d[rd.GetName(i)]=rd.GetValue(i); return d;
    }
    public async Task<List<Dictionary<string,object>>> Many(string sql, params (string,object)[] p) {
        var list=new List<Dictionary<string,object>>(); await using var con=new SqlConnection(Cs); await con.OpenAsync();
        await using var cmd=new SqlCommand(sql,con); foreach(var x in p) cmd.Parameters.AddWithValue(x.Item1,x.Item2??DBNull.Value);
        await using var rd=await cmd.ExecuteReaderAsync(); while(await rd.ReadAsync()){var d=new Dictionary<string,object>();for(int i=0;i<rd.FieldCount;i++)d[rd.GetName(i)]=rd.GetValue(i);list.Add(d);} return list;
    }
    public async Task Exec(string sql, params (string,object)[] p) {
        await using var con=new SqlConnection(Cs); await con.OpenAsync(); await using var cmd=new SqlCommand(sql,con);
        foreach(var x in p) cmd.Parameters.AddWithValue(x.Item1,x.Item2??DBNull.Value); await cmd.ExecuteNonQueryAsync();
    }
}
