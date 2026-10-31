/* EcoEast frontend — connected to EcoEast.API (JWT) */
const API = ["50311", "50312"].includes(location.port) ? location.origin : "https://localhost:50311";
const AUTH_KEY = "ecoeast.auth";
const $ = s => document.querySelector(s);
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));

/* ---------- navigation per role ---------- */
const navConfig = {
 resident: [["Dashboard","▦"],["Request Waste Services","＋"],["Report Environmental Issue","⚑"],["My Service Requests","▤"],["My Issue Reports","◉"],["Waste Collection Schedules","▦"],["Recycling Guide","♻"],["Drop-off Points","⌖"],["Environmental Projects","♧"],["Community Clean-ups & Events","✚"],["Notifications","♢"],["My Profile","♙"]],
 manager: [["Dashboard","▦"],["Service Requests","▤"],["Environmental Issue Reports","⚑"],["Assign Field Workers","♧"],["Field Worker Management","♙"],["Waste Collection Schedules","▦"],["Recycling Guide","♻"],["Analytics & Reports","▥"],["My Profile","♙"]],
 worker: [["Dashboard","▦"],["My Assignments","▤"],["Waste Collection Schedules","▦"],["Issue Inspections","⚑"],["Upload Completion Proof","⇧"],["Notifications","♢"],["My Profile","♙"]],
 admin: [["Dashboard","▦"],["User Accounts","♙"],["Staff Accounts","♧"],["Role Management","⚙"],["Audit Logs","▤"],["System Monitoring","◉"],["My Profile","♙"]]
};
const roleNames = {resident:"Resident", manager:"Manager", worker:"Field Worker", admin:"IT Administrator"};
const state = {auth:null, role:"resident", page:"Dashboard"};

/* ---------- helpers ---------- */
// ASP.NET camelCase turns SCHEDULE_ID into "schedulE_ID"; normalise every key to lowercase, no underscores.
function norm(v){
 if (Array.isArray(v)) return v.map(norm);
 if (v && typeof v === "object") return Object.fromEntries(Object.entries(v).map(([k,x]) => [k.toLowerCase().replace(/_/g,""), norm(x)]));
 return v;
}
function roleKey(roles){
 const r = (roles || []).map(x => String(x).toLowerCase().replace(/[\s_-]/g,""));
 if (r.some(x => x.includes("admin"))) return "admin";
 if (r.some(x => x.includes("manager"))) return "manager";
 if (r.some(x => x.includes("worker") || x.includes("field"))) return "worker";
 return "resident";
}
function tokenValid(t){
 try { const p = JSON.parse(atob(t.split(".")[1].replace(/-/g,"+").replace(/_/g,"/"))); return !p.exp || p.exp*1000 > Date.now(); } catch { return false; }
}
function saveAuth(a){ state.auth = a; localStorage.setItem(AUTH_KEY, JSON.stringify(a)); }
function logout(){ localStorage.removeItem(AUTH_KEY); state.auth = null; showAuth("login"); }
async function api(path, opts = {}){
 const headers = {"Content-Type":"application/json"};
 if (state.auth?.token) headers.Authorization = "Bearer " + state.auth.token;
 let res;
 try { res = await fetch(API + path, {...opts, headers}); }
 catch { throw new Error("Cannot reach the EcoEast API. Make sure it is running at https://localhost:50311."); }
 let body = null;
 try { body = norm(await res.json()); } catch {}
 if (res.status === 401 && state.auth) { logout(); throw new Error("Your session has expired. Please log in again."); }
  if (res.status === 403) throw new Error("You do not have permission to do that. Verify that your account has the required role, then log out and log in again to refresh your JWT token.");
 if (!res.ok) {
  let m = body?.message;
  if (!m && Array.isArray(body?.errors)) m = body.errors.join(" ");
  if (!m && body?.errors) m = Object.values(body.errors).flat().join(" ");
  throw new Error(m || body?.title || `Request failed (${res.status}).`);
 }
 return body;
}
const get = p => api(p);
const post = (p, data) => api(p, {method:"POST", body:JSON.stringify(data)});
function statusClass(n){ n = String(n||"").toLowerCase();
 if (n.includes("complet")||n.includes("resolv")||n.includes("closed")) return "completed";
 if (n.includes("progress")||n.includes("assign")||n.includes("review")) return "progress";
 if (n.includes("reject")||n.includes("cancel")||n.includes("urgent")) return "urgent";
 return "pending"; }
const badge = n => `<span class="status ${statusClass(n)}">${esc(n)}</span>`;
const fmtDate = d => d ? new Date(d).toLocaleDateString("en-ZA",{day:"numeric",month:"short",year:"numeric"}) : "—";
const fmtTime = t => t ? String(t).slice(0,5) : "—";
const locLabel = l => `${[l.streetnumber,l.streetname].filter(Boolean).join(" ") || "Location #"+l.locationid} — ${l.suburbname}`;
const msg = (el, text, ok) => { el.textContent = text; el.className = "msg show " + (ok ? "ok" : "error"); };

/* ---------- UI building blocks ---------- */
function stat(label,value,foot,icon){return `<article class="stat-card"><div class="stat-top"><span>${label}</span><span class="stat-icon">${icon}</span></div><div class="stat-value">${value}</div><div class="stat-foot">${foot}</div></article>`}
function panel(title,body,extra=""){return `<section class="panel"><div class="panel-head"><div><h2>${title}</h2>${extra?`<div class="panel-sub">${extra}</div>`:""}</div></div>${body}</section>`}
function table(headers,rows){ if(!rows.length) return `<div class="empty-note">Nothing to show yet.</div>`;
 return `<table><thead><tr>${headers.map(h=>`<th>${h}</th>`).join("")}</tr></thead><tbody>${rows.map(r=>`<tr>${r.map(v=>`<td>${v}</td>`).join("")}</tr>`).join("")}</tbody></table>`}
const notice = (title, text) => panel(esc(title), `<div class="empty-note">${text}</div>`);
const options = (items, val, label, ph) => `<option value="">${ph}</option>` + items.map(i => `<option value="${esc(i[val])}">${esc(label(i))}</option>`).join("");

/* ---------- pages ---------- */
const pages = {
 async "Dashboard"(){
  if (state.role === "manager") {
   const d = await get("/api/manager/dashboard");
   return `<div class="grid stats-grid">${stat("Total Reports",d.totalreports,"Community reports","⚑")}${stat("Pending Reports",d.pendingreports,"Awaiting review","◷")}${stat("Total Requests",d.totalrequests,"Service requests","▤")}${stat("Active Schedules",d.activeschedules,"Collection schedules","▦")}</div>`;
  }
  if (state.role === "worker") {
   const d = await get("/api/worker/dashboard");
   return `<div class="grid stats-grid">${stat("Assigned Schedules",d.assignedschedulescount,"Active schedules","▦")}${stat("Pending Requests",d.pendingrequestscount,"Requests requiring work","▤")}</div>`;
  }
  if (state.role === "admin") {
   const users = await get("/api/admin/users");
   return `<div class="grid stats-grid">${stat("User Accounts",users.length,"Registered accounts","♙")}</div>${panel("Administration", "Use User Accounts to review the directory and Role Management to assign existing roles.")}`;
  }
  const d = await get("/api/resident/dashboard");
  const [reqs, reps] = await Promise.all([get("/api/resident/service-requests"), get("/api/resident/reports")]);
  const nc = d.nextcollection;
  const recent = [...reqs.slice(0,3).map(r=>["Request: "+esc(r.requesttype), badge(r.status), "EE-REQ-"+r.requestid]), ...reps.slice(0,3).map(r=>["Report: "+esc(r.category), badge(r.status), "EE-REP-"+r.reportid])];
  return `<div class="grid stats-grid">${stat("Total Service Requests",d.totalrequests,d.inprogressrequests+" in progress","▤")}${stat("Completed Requests",d.completedrequests,"Service history","✓")}${stat("Issue Reports",d.totalreports,d.reportsawaitingreview+" awaiting review","⚑")}${stat("Next Waste Collection",nc?esc(nc.collectionday):"—",nc?esc(nc.collectiontype)+" · "+fmtTime(nc.collectiontime):"Add a location to your profile to see this","▦")}</div>
  <div class="grid two-col">${panel("Quick Actions",`<div class="quick-actions"><button class="action-button" data-go="Request Waste Services"><span class="action-symbol">＋</span>Request Waste Service</button><button class="action-button" data-go="Report Environmental Issue"><span class="action-symbol">⚑</span>Report Environmental Issue</button><button class="action-button" data-go="Waste Collection Schedules"><span class="action-symbol">▦</span>View Waste Schedule</button><button class="action-button" data-go="Recycling Guide"><span class="action-symbol">♻</span>Recycling Guide</button></div>`)}
  ${panel("Recent Activity", table(["Activity","Status","Reference"], recent))}</div>`;
 },
 async "Request Waste Services"(){
  const [areas, types] = await Promise.all([get("/api/lookups/service-areas"), get("/api/lookups/request-types")]);
  return panel("Request a Waste Service", `<form id="request-form"><div class="form-grid"><div class="field"><label>Service type</label><select name="requestTypeId" required>${options(types,"requesttypeid",t=>t.requestname,"Choose an option")}</select></div><div class="field"><label>Residential area</label><select name="wsId" required>${options(areas,"wsid",a=>a.suburbname+" — "+a.wastesolname,"Select your area")}</select></div><div class="field" style="grid-column:1/-1"><label>Description</label><textarea name="description" required placeholder="Provide details..."></textarea></div></div><div style="margin-top:14px"><button class="primary-button" type="submit">Submit Request</button></div><div class="msg" id="form-message"></div></form>`, "Your request is saved to the EcoEast database.");
 },
 async "Report Environmental Issue"(){
  const [areas, cats, locs] = await Promise.all([get("/api/lookups/service-areas"), get("/api/lookups/report-categories"), get("/api/lookups/locations")]);
  return panel("Submit an Environmental Issue", `<form id="report-form"><div class="form-grid"><div class="field"><label>Issue category</label><select name="categoryId" required>${options(cats,"repcategoryid",c=>c.categoryname,"Choose an option")}</select></div><div class="field"><label>Residential area</label><select name="wsId" required>${options(areas,"wsid",a=>a.suburbname+" — "+a.wastesolname,"Select your area")}</select></div><div class="field"><label>Location</label><select name="locationId">${options(locs,"locationid",locLabel,"Choose a location (optional)")}</select></div><div class="field" style="grid-column:1/-1"><label>Description</label><textarea name="description" required placeholder="Provide details..."></textarea></div></div><div style="margin-top:14px"><button class="primary-button" type="submit">Submit Report</button></div><div class="msg" id="form-message"></div></form>`, "Your report is saved to the EcoEast database.");
 },
 async "My Service Requests"(){
  const r = await get("/api/resident/service-requests");
  return panel("My Service Requests", table(["Reference","Type","Description","Scheduled","Status"], r.map(x=>["EE-REQ-"+x.requestid, esc(x.requesttype), esc(x.description||"—"), fmtDate(x.scheduleddate), badge(x.status)])));
 },
 async "My Issue Reports"(){
  const r = await get("/api/resident/reports");
  return panel("My Issue Reports", table(["Reference","Category","Description","Submitted","Status"], r.map(x=>["EE-REP-"+x.reportid, esc(x.category), esc(x.description||"—"), fmtDate(x.submissiondate), badge(x.status)])));
 },
  async "Waste Collection Schedules"(){
   const endpoint = state.role === "resident" ? "/api/schedules/areas" : state.role === "manager" ? "/api/manager/schedules" : "/api/worker/schedules";
   const s = await get(endpoint);
   return panel("Waste Collection Schedules", table(["Area","Day","Type","Time","Team"], s.map(x=>[esc(x.suburbname||x.suburbsname), esc(x.collectionday), esc(x.collectiontype), fmtTime(x.collectiontime), esc(x.assignedteam||"—")])));
 },
 async "Recycling Guide"(){
  const g = await get("/api/recycling-guide");
  return panel("Recycling Guide", table(["Item","Recyclable","Disposal rule","Posted"], g.map(x=>[esc(x.categoryname), x.isrecyclable?'<span class="status completed">Yes</span>':'<span class="status urgent">No</span>', esc(x.disposalrule||"—"), fmtDate(x.dateposted)])));
 },
  async "Drop-off Points"(){
   const points = await get("/api/public/dropoff-points");
   return panel("Drop-off Points", table(["Name","Address","Hours","Accepted Materials"], points.map(x=>[esc(x.name), esc(x.address), esc(x.hours), esc(x.materials)])));
  },
  async "Environmental Projects"(){
   const projects = await get("/api/public/projects");
   return panel("Environmental Projects", projects.map(x=>`<article class="panel"><div class="panel-head"><h3>${esc(x.title)}</h3>${badge(x.status)}</div><p>${esc(x.description)}</p><div class="progress"><div class="progress-bar" style="width:${x.progress}%">${x.progress}%</div></div></article>`).join(""));
  },
  async "Community Clean-ups & Events"(){
   const events = await get("/api/public/events");
   return panel("Community Clean-ups & Events", table(["Event","Date","Location","Organizer"], events.map(x=>[esc(x.title), esc(x.date), esc(x.location), esc(x.organizer)])));
  },
 async "Environmental Issue Reports"(){
   const reports = await get("/api/manager/reports");
   return panel("Environmental Issue Reports", table(["Report","Resident","Category","Submitted","Status"], reports.map(x=>["EE-REP-"+x.reportid, esc(x.residentname), esc(x.category), fmtDate(x.submissiondate), badge(x.status)])));
 },
 async "Service Requests"(){
   const requests = await get("/api/worker/service-requests");
   return panel("Service Requests", table(["Request","Resident","Type","Description","Status"], requests.map(x=>["EE-REQ-"+x.requestid, esc(x.residentname), esc(x.requesttype), esc(x.description||"—"), badge(x.status)])));
 },
 async "My Assignments"(){
   const assignments = await get("/api/worker/schedules");
   return panel("My Assignments", table(["Assignment","Request","Date","Time","Collection Type","Description"], assignments.map(x=>[x.scheduleid, "EE-REQ-"+x.requestid, fmtDate(x.collectiondate), fmtTime(x.collectiontime), esc(x.collectiontype), esc(x.description||"—")] )));
 },
  async "Assign Field Workers"(){
   const [requests, workers, types] = await Promise.all([get("/api/worker/service-requests"), get("/api/manager/workers"), get("/api/manager/collection-types")]);
   return panel("Assign Field Workers", `<form id="assignment-form"><div class="form-grid"><div class="field"><label>Service request</label><select name="requestId" required>${options(requests,"requestid",x=>"EE-REQ-"+x.requestid+" — "+(x.description||"No description"),"Choose a request")}</select></div><div class="field"><label>Field worker</label><select name="workerId" required>${options(workers,"workerid",x=>x.teamname||"Worker #"+x.workerid,"Choose a worker")}</select></div><div class="field"><label>Collection type</label><select name="collectionTypeId" required>${options(types,"collectiontypeid",x=>x.name,"Choose a collection type")}</select></div><div class="field"><label>Collection date</label><input name="collectionDate" type="date" required></div><div class="field"><label>Collection time</label><input name="collectionTime" type="time" required></div></div><button class="primary-button" type="submit">Assign Worker</button><div class="msg" id="form-message"></div></form>`);
  },
  async "Field Worker Management"(){
   const [dashboard, schedules] = await Promise.all([get("/api/worker/dashboard"), get("/api/worker/schedules")]);
   return `<div class="grid stats-grid">${stat("Assigned Schedules",dashboard.assignedschedulescount,"Active assignments","▦")}${stat("Pending Requests",dashboard.pendingrequestscount,"Requests requiring work","▤")}</div>${panel("Field Worker Assignments", table(["Assignment","Request","Date","Time","Type"], schedules.map(x=>[x.scheduleid, "EE-REQ-"+x.requestid, fmtDate(x.collectiondate), fmtTime(x.collectiontime), esc(x.collectiontype)])))} `;
  },
  async "Analytics & Reports"(){
   const [dashboard, reports] = await Promise.all([get("/api/manager/dashboard"), get("/api/manager/reports")]);
   return `<div class="grid stats-grid">${stat("Total Reports",dashboard.totalreports,"Community reports","⚑")}${stat("Pending Reports",dashboard.pendingreports,"Awaiting review","◷")}${stat("Total Requests",dashboard.totalrequests,"Service requests","▤")}${stat("Active Schedules",dashboard.activeschedules,"Collection schedules","▦")}</div>${panel("Report Analytics", table(["Report","Category","Resident","Status","Submitted"], reports.map(x=>["EE-REP-"+x.reportid, esc(x.category), esc(x.residentname), badge(x.status), fmtDate(x.submissiondate)])))} `;
  },
 async "User Accounts"(){
   const users = await get("/api/admin/users");
   return panel("User Accounts", table(["Username","Email","Name","Roles"], users.map(x=>[esc(x.username), esc(x.email), esc([x.firstname,x.lastname].filter(Boolean).join(" ")||"—"), esc((x.roles||[]).join(", "))])));
 },
  async "Role Management"(){
   const users = await get("/api/admin/users");
   return panel("Role Management", `<form id="role-form"><div class="form-grid"><div class="field"><label>User</label><select name="userId" required>${options(users,"id",u=>u.username||u.email,"Choose a user")}</select></div><div class="field"><label>Role</label><select name="roleName" required><option value="">Choose a role</option><option>Resident</option><option>FieldWorker</option><option>Manager</option><option>Admin</option></select></div></div><button class="primary-button" type="submit">Assign Role</button><div class="msg" id="form-message"></div></form>`);
  },
  async "Staff Accounts"(){
   const staff = await get("/api/admin/staff");
   return panel("Staff Accounts", table(["Name","Username","Email","Role","Phone"], staff.map(x=>[esc([x.firstname,x.lastname].filter(Boolean).join(" ")), esc(x.username), esc(x.email), badge(x.role), esc(x.phone||"—")] )));
  },
  async "Audit Logs"(){
   const logs = await get("/api/admin/audit-logs");
   return panel("Audit Logs", logs.map(x=>`<article class="panel"><div class="panel-head"><h3>${esc(x.action)}</h3><small>${fmtDate(x.timestamp)}</small></div><p>User: <code>${esc(x.user)}</code> — ${esc(x.details)}</p></article>`).join(""));
  },
  async "System Monitoring"(){
   const health = await get("/api/admin/system-health");
   return `<div class="grid stats-grid">${stat("Status",health.status,"API health","◉")}${stat("Database",health.database,"Connection state","▣")}${stat("Server Time",fmtDate(health.servertime),"UTC","◷")}</div>`;
  },
 async "My Profile"(){
   const p = await get("/api/profile");
   return panel("My Profile", `<form id="profile-form"><div class="form-grid"><div class="field"><label>First name</label><input name="firstName" value="${esc(p.firstname||"")}" required></div><div class="field"><label>Last name</label><input name="lastName" value="${esc(p.lastname||"")}" required></div><div class="field"><label>Phone</label><input name="phone" value="${esc(p.phonenumber||"")}"></div><div class="field"><label>Location ID</label><input name="locationId" type="number" value="${esc(p.locationid||"")}"></div></div><button class="primary-button" type="submit">Save Profile</button><div class="msg" id="form-message"></div></form>`);
  },
  async "Issue Inspections"(){
   const inspections = await get("/api/worker/inspections");
   if (!inspections.length) return notice("Issue Inspections", "No pending inspections found.");
   return panel("Issue Inspections", inspections.map(x=>`<article class="panel" data-inspection="${x.id}"><h3>[#${x.id}] ${esc(x.category)}</h3><p><b>Resident:</b> ${esc(x.residentname)}</p><p><b>Description:</b> ${esc(x.description||"No description provided.")}</p><p><b>Submitted:</b> ${fmtDate(x.datesubmitted)}</p><textarea data-notes placeholder="Enter inspection notes..."></textarea><button class="primary-button" data-submit-inspection="${x.id}">Issue Inspection Result</button></article>`).join(""));
  },
  async "Upload Completion Proof"(){
   const assignments = await get("/api/worker/schedules");
   return panel("Upload Completion Proof", `<form id="completion-proof-form"><div class="form-grid"><div class="field" style="grid-column:1/-1"><label>Assigned task</label><select name="requestId" required>${options(assignments,"requestid",x=>"#"+x.requestid+" — "+(x.description||"Assigned task"),"Select an assigned task")}</select></div><div class="field" style="grid-column:1/-1"><label>Proof image URL (optional)</label><input name="proofImageUrl" placeholder="https://..."></div><div class="field" style="grid-column:1/-1"><label>Completion notes</label><textarea name="notes" placeholder="Describe the completed work..."></textarea></div></div><button class="primary-button" type="submit">Submit Completion Proof</button><div class="msg" id="form-message"></div></form>`);
  },
  async "Notifications"(){
   const notifications = await get(state.role === "resident" ? "/api/public/notifications" : "/api/worker/notifications");
   if (!notifications.length) return notice("Notifications", "No new notifications at this time.");
   return panel("Notifications", notifications.map(n=>`<article class="panel" data-notification="${n.id}"><div><h3>${esc(n.title)}</h3><small>${fmtDate(n.date)}</small></div><p>${esc(n.message)}</p><button class="secondary-button" data-read-notification="${n.id}">Mark as Read</button></article>`).join(""));
 }
};
const pageForms = {
 "Request Waste Services": {id:"request-form", url:"/api/resident/service-requests", body:f=>({wsId:+f.wsId, requestTypeId:+f.requestTypeId, description:f.description}), next:"My Service Requests"},
  "Report Environmental Issue": {id:"report-form", url:"/api/resident/reports", body:f=>({wsId:+f.wsId, categoryId:+f.categoryId, description:f.description, locationId:f.locationId?+f.locationId:null}), next:"My Issue Reports"},
  "Assign Field Workers": {id:"assignment-form", url:"/api/manager/assignments", body:f=>({workerId:+f.workerId, requestId:+f.requestId, collectionTypeId:+f.collectionTypeId, collectionDate:f.collectionDate, collectionTime:f.collectionTime}), next:"Assign Field Workers"},
  "Upload Completion Proof": {id:"completion-proof-form", url:"/api/worker/completion-proof", body:f=>({requestId:+f.requestId, proofImageUrl:f.proofImageUrl||null, notes:f.notes||null}), next:"Upload Completion Proof"},
  "My Profile": {id:"profile-form", url:"/api/profile", body:f=>({firstName:f.firstName, lastName:f.lastName, phone:f.phone||null, locationId:f.locationId?+f.locationId:null}), next:"My Profile"},
  "Role Management": {id:"role-form", url:"/api/admin/users/assign-role", body:f=>({userId:f.userId, roleName:f.roleName}), next:"Role Management"}
};

/* ---------- shell ---------- */
function navRender(){
 $("#main-nav").innerHTML = navConfig[state.role].map(([label,icon])=>`<a href="#" class="nav-link ${state.page===label?"active":""}" data-page="${esc(label)}"><span class="nav-icon">${icon}</span><span>${esc(label)}</span></a>`).join("");
 $("#main-nav").querySelectorAll("a").forEach(a=>a.addEventListener("click",e=>{e.preventDefault();state.page=a.dataset.page;render();closeMenu();}));
 const u = state.auth.user, name = [u.firstname,u.lastname].filter(Boolean).join(" ") || u.username;
 $("#user-role").textContent = roleNames[state.role]; $("#user-name").textContent = name; $("#user-avatar").textContent = (name[0]||"?").toUpperCase();
}
async function render(){
 navRender();
 const role = roleNames[state.role], u = state.auth.user;
 $("#crumb-title").textContent = state.page;
 $("#page-title").textContent = state.page==="Dashboard" ? role+" Dashboard" : state.page;
 $("#page-subtitle").textContent = state.page==="Dashboard" ? `Welcome back, ${u.firstname||u.username}.` : "Manage EcoEast services and information.";
 $("#page-eyebrow").textContent = state.role==="resident" ? "COMMUNITY ENVIRONMENTAL SERVICES" : "ECOEAST · "+role.toUpperCase();
 const box = $("#page-content"), page = state.page;
 box.innerHTML = `<div class="loading">Loading…</div>`;
 try {
  const handler = pages[page];
  const html = handler ? await handler() : notice(page, "This page is not connected to the API yet.");
  if (state.page !== page) return;                       // user navigated away while loading
  box.innerHTML = html;
 } catch(err){ if (state.auth) box.innerHTML = `<div class="msg show error">${esc(err.message)}</div>`; return; }
 box.querySelectorAll("[data-go]").forEach(b=>b.addEventListener("click",()=>{state.page=b.dataset.go;render()}));
 const cfg = pageForms[page];
 if (cfg) { const form = document.getElementById(cfg.id); if (form) form.addEventListener("submit", async e => {
  e.preventDefault(); const m = $("#form-message"), btn = form.querySelector("button[type=submit]"); btn.disabled = true;
  try { const r = await post(cfg.url, cfg.body(Object.fromEntries(new FormData(form)))); msg(m, r.message || "Saved.", true); form.reset(); setTimeout(()=>{ if(state.page===page){state.page=cfg.next; render();} }, 900); }
  catch(err){ msg(m, err.message, false); btn.disabled = false; } }); }
  box.querySelectorAll("[data-submit-inspection]").forEach(button => button.addEventListener("click", async () => {
   const reportId = +button.dataset.submitInspection;
   const notes = button.closest("[data-inspection]")?.querySelector("[data-notes]")?.value || "";
   button.disabled = true;
   try { await post("/api/worker/inspections", {reportId, notes, isApproved:true}); await render(); }
   catch(err){ alert(err.message); button.disabled = false; }
  }));
  box.querySelectorAll("[data-read-notification]").forEach(button => button.addEventListener("click", async () => {
   button.disabled = true;
   try { await post(`/api/worker/notifications/${button.dataset.readNotification}/read`); await render(); }
   catch(err){ alert(err.message); button.disabled = false; }
  }));
}
function closeMenu(){$("#sidebar").classList.remove("open");$("#mobile-scrim").classList.remove("show")}
$("#menu-toggle").addEventListener("click",()=>{$("#sidebar").classList.toggle("open");$("#mobile-scrim").classList.toggle("show")});
$("#mobile-scrim").addEventListener("click",closeMenu);
$("#logout-button").addEventListener("click",logout);
$("#today-label").textContent = new Date().toLocaleDateString("en-ZA",{weekday:"long",day:"numeric",month:"long",year:"numeric"});

/* ---------- login / register ---------- */
function showApp(){ state.role = roleKey(state.auth.user.roles); state.page = "Dashboard"; $("#auth-screen").hidden = true; $("#app-shell").hidden = false; render(); }
async function showAuth(mode){
 $("#app-shell").hidden = true; const box = $("#auth-screen"); box.hidden = false;
 const brand = `<a class="brand" href="#"><span class="brand-mark">♻</span><span>Eco<span>East</span></span></a>`;
 if (mode === "login") {
  box.innerHTML = `<div class="auth-card">${brand}<h1>Log in</h1><p class="panel-sub">Sign in to your EcoEast account.</p><form id="login-form"><div class="field"><label>Username</label><input name="userName" required autocomplete="username"></div><div class="field"><label>Password</label><input name="password" type="password" required autocomplete="current-password"></div><button class="primary-button" type="submit">Log in</button><div class="msg" id="auth-msg"></div></form><div class="auth-switch">New resident? <a id="go-register">Create an account</a></div></div>`;
  $("#go-register").onclick = () => showAuth("register");
  $("#login-form").addEventListener("submit", async e => { e.preventDefault(); const btn = e.target.querySelector("button"); btn.disabled = true;
   try { const f = Object.fromEntries(new FormData(e.target)); const r = await post("/api/auth/login", {userName:f.userName, password:f.password}); saveAuth({token:r.token, user:r.user}); showApp(); }
   catch(err){ msg($("#auth-msg"), err.message, false); btn.disabled = false; } });
 } else {
  box.innerHTML = `<div class="auth-card">${brand}<h1>Create resident account</h1><p class="panel-sub">Your account is saved to the EcoEast database.</p><form id="reg-form"><div class="form-grid"><div class="field"><label>First name</label><input name="firstName" required></div><div class="field"><label>Last name</label><input name="lastName" required></div><div class="field"><label>Username</label><input name="userName" required autocomplete="username"></div><div class="field"><label>Email</label><input name="email" type="email" required></div><div class="field"><label>Password (min 8 characters)</label><input name="password" type="password" minlength="8" required autocomplete="new-password"></div><div class="field"><label>Phone</label><input name="phone"></div><div class="field" style="grid-column:1/-1"><label>Your location</label><select name="locationId" id="reg-loc"><option value="">Loading locations…</option></select></div></div><button class="primary-button" type="submit">Create account</button><div class="msg" id="auth-msg"></div></form><div class="auth-switch">Already registered? <a id="go-login">Log in</a></div></div>`;
  $("#go-login").onclick = () => showAuth("login");
  get("/api/lookups/locations").then(l => { $("#reg-loc").innerHTML = options(l,"locationid",locLabel,"Choose a location (optional)"); }).catch(()=>{ $("#reg-loc").innerHTML = `<option value="">Locations unavailable</option>`; });
  $("#reg-form").addEventListener("submit", async e => { e.preventDefault(); const btn = e.target.querySelector("button"); btn.disabled = true;
   try { const f = Object.fromEntries(new FormData(e.target)); await post("/api/auth/register-resident", {...f, locationId: f.locationId ? +f.locationId : null, phone: f.phone || null});
    const r = await post("/api/auth/login", {userName:f.userName, password:f.password}); saveAuth({token:r.token, user:r.user}); showApp(); }
   catch(err){ msg($("#auth-msg"), err.message, false); btn.disabled = false; } });
 }
}

/* ---------- start ---------- */
(function init(){
 try { const a = JSON.parse(localStorage.getItem(AUTH_KEY) || "null"); if (a?.token && tokenValid(a.token)) { state.auth = a; return showApp(); } } catch {}
 showAuth("login");
})();
