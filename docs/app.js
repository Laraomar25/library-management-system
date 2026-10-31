(() => {
  "use strict";

  // ---------- Storage (browser only, safe if blocked) ----------
  const KEY = "ravan-library-v1";
  const SESSION_KEY = "ravan-library-user";
  const mem = {};
  const store = {
    get(k, s = localStorage) { try { return s.getItem(k); } catch { return mem[k] ?? null; } },
    set(k, v, s = localStorage) { try { s.setItem(k, v); return true; } catch (e) { mem[k] = v; return !(e && e.name === "QuotaExceededError"); } },
    del(k, s = localStorage) { try { s.removeItem(k); } catch { delete mem[k]; } },
  };

  const today = () => new Date().toISOString().slice(0, 10);
  const addDays = (d, n) => { const x = new Date(d + "T00:00:00"); x.setDate(x.getDate() + n); return x.toISOString().slice(0, 10); };

  function seed() {
    const t = today();
    return {
      nextBookId: 11,
      nextIssueId: 3,
      users: [{ username: "demo", email: "demo@ravan.library", password: "demo1234", hashed: false }],
      books: [
        { id: 7, title: "The Kite Runner", author: "Khaled Hosseini", genre: "Historical Fiction", quantity: 4, cover: "covers/kite-runner.jpg" },
        { id: 8, title: "The Hobbit", author: "J.R.R. Tolkien", genre: "Fantasy", quantity: 5, cover: "covers/hobbit.jpg" },
        { id: 9, title: "Pride and Prejudice", author: "Jane Austen", genre: "Romance", quantity: 10, cover: "covers/pride-and-prejudice.jpg" },
        { id: 10, title: "The Little Prince", author: "Antoine de Saint-Exupéry", genre: "Fantasy", quantity: 7, cover: "covers/little-prince.jpg" },
      ],
      issued: [
        { id: 2, bookId: 8, title: "The Hobbit", name: "Sample Student", sid: "2024001", phone: "0500 000 00 01", issueDate: addDays(t, -3), returnDate: addDays(t, 4) },
        { id: 1, bookId: 9, title: "Pride and Prejudice", name: "Demo Reader", sid: "2024002", phone: "0500 000 00 02", issueDate: addDays(t, -12), returnDate: addDays(t, -5) },
      ],
    };
  }

  let db;
  try { db = JSON.parse(store.get(KEY)) || seed(); } catch { db = seed(); }
  const save = () => {
    if (!store.set(KEY, JSON.stringify(db))) toast("Your browser storage is full. Try a smaller cover image.", true);
  };

  async function hash(pw) {
    try {
      const buf = await crypto.subtle.digest("SHA-256", new TextEncoder().encode("ravan:" + pw));
      return [...new Uint8Array(buf)].map(b => b.toString(16).padStart(2, "0")).join("");
    } catch { return null; }
  }
  async function checkPassword(user, pw) {
    if (!user.hashed) return user.password === pw;
    return user.password === await hash(pw);
  }

  // ---------- Helpers ----------
  const $ = (s, el = document) => el.querySelector(s);
  const $$ = (s, el = document) => [...el.querySelectorAll(s)];
  const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const norm = s => String(s ?? "").trim().toLowerCase();
  const fmtDate = d => d ? new Date(d + "T00:00:00").toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" }) : "";

  function toast(msg, error = false) {
    const el = document.createElement("div");
    el.className = "toast" + (error ? " error" : "");
    el.textContent = msg;
    $("#toasts").append(el);
    setTimeout(() => el.remove(), 3200);
  }

  function confirmBox(title, text) {
    const dlg = $("#confirmDialog");
    $("#confirmTitle").textContent = title;
    $("#confirmText").textContent = text;
    if (typeof dlg.showModal !== "function") return Promise.resolve(window.confirm(title + "\n" + text));
    dlg.returnValue = "";
    dlg.showModal();
    return new Promise(res => dlg.addEventListener("close", () => res(dlg.returnValue === "yes"), { once: true }));
  }

  function coverHTML(book, alt = true) {
    if (book && book.cover) return `<img src="${esc(book.cover)}" alt="${alt ? "Cover of " + esc(book.title) : ""}">`;
    return `<span class="ph">${esc(book ? book.title : "No cover")}</span>`;
  }
  const bookById = id => db.books.find(b => b.id === id);

  // ---------- Screens ----------
  let currentUser = store.get(SESSION_KEY, sessionStorage);

  function showAuth(which = "register") {
    $("#app").hidden = true;
    $("#mobileNav").hidden = true;
    $("#auth").hidden = false;
    $("#loginForm").hidden = which !== "login";
    $("#registerForm").hidden = which !== "register";
    const f = which === "login" ? $("#loginForm") : $("#registerForm");
    setTimeout(() => f.querySelector("input").focus(), 50);
  }

  function showApp() {
    $("#auth").hidden = true;
    $("#app").hidden = false;
    $("#mobileNav").hidden = false;
    $("#helloName").textContent = "Hello, " + currentUser;
    go(location.hash.slice(1) || "home", false);
  }

  const views = ["home", "books", "issue", "return", "chart"];
  function go(view, push = true) {
    if (!views.includes(view)) view = "home";
    $$("[data-page]").forEach(p => (p.hidden = p.dataset.page !== view));
    $$(".nav-btn[data-view]").forEach(b => {
      const on = b.dataset.view === view;
      b.classList.toggle("active", on);
      if (on) b.setAttribute("aria-current", "page"); else b.removeAttribute("aria-current");
    });
    if (push) history.replaceState(null, "", "#" + view);
    render[view]();
    window.scrollTo({ top: 0 });
  }

  // ---------- Auth ----------
  $$("[data-go]").forEach(b => b.addEventListener("click", () => showAuth(b.dataset.go)));
  $$("[data-demo]").forEach(b => b.addEventListener("click", () => signIn("demo")));

  function signIn(name) {
    currentUser = name;
    store.set(SESSION_KEY, name, sessionStorage);
    toast("Login successful!");
    showApp();
  }

  $("#loginForm").addEventListener("submit", async e => {
    e.preventDefault();
    const f = e.target;
    const u = f.username.value.trim(), p = f.password.value;
    const user = db.users.find(x => norm(x.username) === norm(u));
    if (!u || !p) return toast("Please enter your username and password.", true);
    if (!user || !(await checkPassword(user, p))) return toast("Invalid username or password.", true);
    f.reset();
    signIn(user.username);
  });

  $("#registerForm").addEventListener("submit", async e => {
    e.preventDefault();
    const f = e.target;
    const u = f.username.value.trim(), em = f.email.value.trim(), p = f.password.value;
    if (!u || !em || !p) return toast("Please fill all the fields.", true);
    if (!/^\S+@\S+\.\S+$/.test(em)) return toast("Please enter a valid email.", true);
    if (db.users.some(x => norm(x.username) === norm(u))) return toast("That username is taken.", true);
    const h = await hash(p);
    db.users.push({ username: u, email: em, password: h ?? p, hashed: !!h });
    save();
    f.reset();
    toast("Registered successfully!");
    showAuth("login");
    $("#loginForm").username.value = u;
    $("#loginForm").password.focus();
  });

  $("#logoutBtn").addEventListener("click", async () => {
    if (!(await confirmBox("Log out", "Are you sure you want to logout?"))) return;
    currentUser = null;
    store.del(SESSION_KEY, sessionStorage);
    history.replaceState(null, "", location.pathname);
    showAuth("login");
  });

  $$(".nav-btn[data-view], .tile[data-view]").forEach(b => b.addEventListener("click", () => go(b.dataset.view)));

  $("#resetBtn").addEventListener("click", async () => {
    if (!(await confirmBox("Reset demo data", "This puts the books and loans back to how they started. Accounts you created will be removed."))) return;
    db = seed(); save();
    toast("Demo data reset.");
    if (!db.users.some(x => x.username === currentUser)) signIn("demo"); else go(location.hash.slice(1) || "home");
  });

  // ---------- Render ----------
  const render = {};

  render.home = () => {
    $("#statTitles").textContent = db.books.length;
    $("#statCopies").textContent = db.books.reduce((s, b) => s + (+b.quantity || 0), 0);
    $("#statIssued").textContent = db.issued.length;
    $("#statOverdue").textContent = db.issued.filter(i => i.returnDate < today()).length;
    $("#shelf").innerHTML = db.books.length
      ? db.books.map(b => `<div class="book"><div class="book-cover">${coverHTML(b)}</div><div><div class="book-title">${esc(b.title)}</div><div class="book-meta">${esc(b.author)} · ${b.quantity} left</div></div></div>`).join("")
      : `<p class="muted">No books yet. Add one from <button class="link" data-jump="books">Add Book</button>.</p>`;
    $$("[data-jump]").forEach(b => b.addEventListener("click", () => go(b.dataset.jump)));
  };

  // Books
  let selectedBookId = -1;
  let pendingCover = null; // data URL chosen in the form
  const bookForm = $("#bookForm");

  function fillGenres() {
    const genres = [...new Set(db.books.map(b => b.genre))].sort();
    $("#genreList").innerHTML = genres.map(g => `<option value="${esc(g)}">`).join("");
  }

  function setPreview(src) {
    $("#bookCoverPreview").innerHTML = src ? `<img src="${esc(src)}" alt="Selected cover">` : "<span>No cover</span>";
  }

  function clearBookForm() {
    bookForm.reset();
    selectedBookId = -1;
    pendingCover = null;
    setPreview(null);
    $$("#booksTable tbody tr").forEach(r => r.classList.remove("selected"));
  }

  render.books = () => {
    fillGenres();
    const q = norm($("#bookSearch").value);
    const rows = db.books.filter(b => !q || [b.title, b.author, b.genre].some(v => norm(v).includes(q)));
    $("#booksTable tbody").innerHTML = rows.length
      ? rows.map(b => `<tr data-id="${b.id}" tabindex="0" class="${b.id === selectedBookId ? "selected" : ""}"><td>${b.id}</td><td>${esc(b.title)}</td><td>${esc(b.author)}</td><td>${esc(b.genre)}</td><td class="num">${b.quantity}</td></tr>`).join("")
      : `<tr class="empty"><td colspan="5">No books found.</td></tr>`;
  };

  function pickBookRow(tr) {
    const b = bookById(+tr.dataset.id);
    if (!b) return;
    selectedBookId = b.id;
    pendingCover = null;
    bookForm.elements.title.value = b.title;
    bookForm.elements.author.value = b.author;
    bookForm.elements.genre.value = b.genre;
    bookForm.elements.quantity.value = b.quantity;
    setPreview(b.cover);
    $$("#booksTable tbody tr").forEach(r => r.classList.toggle("selected", r === tr));
    if (window.innerWidth < 1100) bookForm.scrollIntoView({ behavior: "smooth", block: "start" });
  }
  $("#booksTable tbody").addEventListener("click", e => { const tr = e.target.closest("tr[data-id]"); if (tr) pickBookRow(tr); });
  $("#booksTable tbody").addEventListener("keydown", e => { if (e.key === "Enter" || e.key === " ") { const tr = e.target.closest("tr[data-id]"); if (tr) { e.preventDefault(); pickBookRow(tr); } } });
  $("#bookSearch").addEventListener("input", render.books);

  $("#bookCoverInput").addEventListener("change", e => {
    const file = e.target.files[0];
    e.target.value = "";
    if (!file) return;
    if (!file.type.startsWith("image/")) return toast("Please choose an image file.", true);
    const img = new Image();
    const url = URL.createObjectURL(file);
    img.onload = () => {
      const max = 420, s = Math.min(1, max / img.width);
      const c = document.createElement("canvas");
      c.width = Math.round(img.width * s); c.height = Math.round(img.height * s);
      c.getContext("2d").drawImage(img, 0, 0, c.width, c.height);
      pendingCover = c.toDataURL("image/jpeg", 0.82);
      URL.revokeObjectURL(url);
      setPreview(pendingCover);
    };
    img.onerror = () => toast("Could not read that image.", true);
    img.src = url;
  });

  function readBookForm() {
    const title = bookForm.elements.title.value.trim(), author = bookForm.elements.author.value.trim(), genre = bookForm.elements.genre.value.trim(), qty = bookForm.elements.quantity.value.trim();
    if (!title || !author || !genre || qty === "") { toast("Please fill all fields.", true); return null; }
    const quantity = Number(qty);
    if (!Number.isInteger(quantity) || quantity < 0) { toast("Quantity must be a whole number, 0 or more.", true); return null; }
    return { title, author, genre, quantity };
  }

  bookForm.addEventListener("submit", e => {
    e.preventDefault();
    const data = readBookForm(); if (!data) return;
    db.books.push({ id: db.nextBookId++, ...data, cover: pendingCover });
    save(); toast("Book Added!"); clearBookForm(); render.books();
  });
  bookForm.addEventListener("click", async e => {
    const action = e.target.closest("[data-action]")?.dataset.action;
    if (action === "clear") return clearBookForm();
    if (action === "update") {
      if (selectedBookId === -1) return toast("Select a book to update.", true);
      const data = readBookForm(); if (!data) return;
      const b = bookById(selectedBookId);
      Object.assign(b, data);
      if (pendingCover) b.cover = pendingCover;
      db.issued.forEach(i => { if (i.bookId === b.id) i.title = b.title; });
      save(); toast("Book Updated."); clearBookForm(); render.books();
    }
    if (action === "delete") {
      if (selectedBookId === -1) return toast("Select a book to delete.", true);
      const b = bookById(selectedBookId);
      if (db.issued.some(i => i.bookId === b.id)) return toast("This book is on loan. Take it back before deleting it.", true);
      if (!(await confirmBox("Delete book", `Delete “${b.title}”?`))) return;
      db.books = db.books.filter(x => x.id !== b.id);
      save(); toast("Book Deleted."); clearBookForm(); render.books();
    }
  });

  // Issue
  let issueBookId = -1;
  const issueForm = $("#issueForm");

  function issuedRows(list) {
    return list.length
      ? list.map(i => {
          const late = i.returnDate < today();
          return `<tr data-id="${i.id}" tabindex="0"><td>${esc(i.title)}</td><td>${esc(i.name)}</td><td>${esc(i.sid)}</td><td>${esc(i.phone)}</td><td>${fmtDate(i.issueDate)}</td><td>${late ? `<span class="tag-late">${fmtDate(i.returnDate)} · overdue</span>` : fmtDate(i.returnDate)}</td></tr>`;
        }).join("")
      : `<tr class="empty"><td colspan="6">No books on loan right now.</td></tr>`;
  }
  const sortedIssued = () => [...db.issued].sort((a, b) => b.id - a.id);

  function resetIssue() {
    issueForm.reset();
    $("#issueSearch").value = "";
    issueBookId = -1;
    $("#issueFound").hidden = true;
    issueForm.elements.issueDate.value = today();
    issueForm.elements.returnDate.value = addDays(today(), 7);
  }

  render.issue = () => {
    $("#titleList").innerHTML = db.books.map(b => `<option value="${esc(b.title)}">`).join("");
    if (!issueForm.elements.issueDate.value) resetIssue();
    $("#issuedTable tbody").innerHTML = issuedRows(sortedIssued());
  };

  function searchIssue() {
    const q = norm($("#issueSearch").value);
    if (!q) return toast("Enter a book title to search.", true);
    const b = db.books.find(x => norm(x.title) === q) || db.books.find(x => norm(x.title).includes(q));
    if (!b) { issueBookId = -1; $("#issueFound").hidden = true; return toast("Book not found.", true); }
    issueBookId = b.id;
    $("#issueSearch").value = b.title;
    $("#issueCover").innerHTML = coverHTML(b);
    $("#issueTitle").textContent = b.title;
    $("#issueAuthor").textContent = b.author;
    $("#issueQty").textContent = b.quantity > 0 ? b.quantity : "None left";
    $("#issueFound").hidden = false;
    issueForm.elements.name.focus();
  }
  $("#issueSearchBtn").addEventListener("click", searchIssue);
  $("#issueSearch").addEventListener("keydown", e => { if (e.key === "Enter") { e.preventDefault(); searchIssue(); } });

  issueForm.addEventListener("submit", e => {
    e.preventDefault();
    if (issueBookId === -1) return toast("Search and select a book first.", true);
    const b = bookById(issueBookId);
    const name = issueForm.elements.name.value.trim(), sid = issueForm.elements.sid.value.trim(), phone = issueForm.elements.phone.value.trim();
    if (!name || !sid || !phone) return toast("Please fill all borrower details.", true);
    if (b.quantity <= 0) return toast("No copies of this book are left.", true);
    const issueDate = issueForm.elements.issueDate.value || today(), returnDate = issueForm.elements.returnDate.value || addDays(issueDate, 7);
    if (returnDate < issueDate) return toast("The return date must be after the issue date.", true);
    db.issued.push({ id: db.nextIssueId++, bookId: b.id, title: b.title, name, sid, phone, issueDate, returnDate });
    b.quantity -= 1;
    save(); toast("Book issued successfully!"); resetIssue(); render.issue();
  });

  // Return
  let returnId = -1;
  const returnForm = $("#returnForm");

  render.return = () => {
    $("#returnTable tbody").innerHTML = issuedRows(sortedIssued());
  };

  function showReturn(i) {
    returnId = i.id;
    const b = bookById(i.bookId);
    $("#returnCover").innerHTML = coverHTML(b || { title: i.title });
    $("#returnTitle").textContent = i.title;
    $("#returnName").textContent = i.name;
    $("#returnPhone").textContent = i.phone;
    $("#returnIssued").textContent = fmtDate(i.issueDate);
    $("#returnDue").textContent = fmtDate(i.returnDate) + (i.returnDate < today() ? " (overdue)" : "");
    $("#returnFound").hidden = false;
    $("#returnSearch").value = i.sid;
  }
  function searchReturn() {
    const q = $("#returnSearch").value.trim();
    if (!q) return toast("Please enter a valid ID number to search.", true);
    const i = sortedIssued().find(x => x.sid.trim() === q);
    if (!i) { returnId = -1; $("#returnFound").hidden = true; return toast("No active issued book found with this ID.", true); }
    showReturn(i);
  }
  $("#returnSearchBtn").addEventListener("click", searchReturn);
  $("#returnSearch").addEventListener("keydown", e => { if (e.key === "Enter") { e.preventDefault(); searchReturn(); } });
  const pickReturnRow = tr => { const i = db.issued.find(x => x.id === +tr.dataset.id); if (i) showReturn(i); };
  $("#returnTable tbody").addEventListener("click", e => { const tr = e.target.closest("tr[data-id]"); if (tr) pickReturnRow(tr); });
  $("#returnTable tbody").addEventListener("keydown", e => { if (e.key === "Enter") { const tr = e.target.closest("tr[data-id]"); if (tr) pickReturnRow(tr); } });

  returnForm.addEventListener("submit", e => {
    e.preventDefault();
    if (returnId === -1) return toast("Please search for an issued book by ID first.", true);
    const i = db.issued.find(x => x.id === returnId);
    const b = bookById(i.bookId);
    if (b) b.quantity += 1;
    db.issued = db.issued.filter(x => x.id !== returnId);
    returnId = -1;
    save(); toast("Book returned successfully!");
    $("#returnFound").hidden = true; $("#returnSearch").value = "";
    render.return();
  });

  // Chart: titles per genre (one series, so no legend; values in tooltip + table)
  let chartView = "chart";
  $$("[data-chartview]").forEach(b => b.addEventListener("click", () => {
    chartView = b.dataset.chartview;
    $$("[data-chartview]").forEach(x => { const on = x === b; x.classList.toggle("active", on); x.setAttribute("aria-pressed", on); });
    render.chart();
  }));

  render.chart = () => {
    const map = new Map();
    db.books.forEach(b => {
      const g = b.genre.trim() || "Other";
      const m = map.get(g) || { genre: g, titles: 0, copies: 0 };
      m.titles++; m.copies += +b.quantity || 0; map.set(g, m);
    });
    const data = [...map.values()].sort((a, b) => b.titles - a.titles || a.genre.localeCompare(b.genre));

    $("#chartTable tbody").innerHTML = data.map(d => `<tr><td>${esc(d.genre)}</td><td class="num">${d.titles}</td><td class="num">${d.copies}</td></tr>`).join("") || `<tr class="empty"><td colspan="3">No books yet.</td></tr>`;
    $("#chartTable").hidden = chartView !== "table";
    $("#chartBox").hidden = chartView !== "chart";
    $("#chartTip").hidden = true;
    if (chartView !== "chart") return;
    if (!data.length) { $("#chartBox").innerHTML = `<p class="muted">No books yet.</p>`; return; }

    const W = 720, H = 340, padL = 36, padR = 8, padT = 12, padB = 56;
    const max = Math.max(...data.map(d => d.titles));
    const top = Math.max(2, Math.ceil(max));
    const step = top <= 5 ? 1 : Math.ceil(top / 5);
    const yTop = Math.ceil(top / step) * step;
    const iw = W - padL - padR, ih = H - padT - padB;
    const band = iw / data.length;
    const bw = Math.min(56, band * 0.5);
    const y = v => padT + ih - (v / yTop) * ih;

    let grid = "";
    for (let v = 0; v <= yTop; v += step) {
      grid += `<line class="${v === 0 ? "base" : "grid"}" x1="${padL}" x2="${W - padR}" y1="${y(v)}" y2="${y(v)}"/>` +
              `<text class="axis" x="${padL - 10}" y="${y(v) + 4}" text-anchor="end">${v}</text>`;
    }
    const cols = data.map((d, i) => {
      const cx = padL + band * i + band / 2, x = cx - bw / 2, yy = y(d.titles), h = padT + ih - yy, r = Math.min(4, h);
      const path = `M${x},${padT + ih} V${yy + r} Q${x},${yy} ${x + r},${yy} H${x + bw - r} Q${x + bw},${yy} ${x + bw},${yy + r} V${padT + ih} Z`;
      const words = d.genre.split(/\s+/);
      const lines = words.length > 1 && d.genre.length > 12 ? [words.slice(0, Math.ceil(words.length / 2)).join(" "), words.slice(Math.ceil(words.length / 2)).join(" ")] : [d.genre];
      const label = lines.map((l, k) => `<tspan x="${cx}" dy="${k ? 15 : 0}">${esc(l)}</tspan>`).join("");
      return `<g class="col" tabindex="0" role="img" aria-label="${esc(d.genre)}: ${d.titles} title${d.titles === 1 ? "" : "s"}, ${d.copies} copies" data-i="${i}">
        <path class="bar" d="${path}"/>
        <rect class="hit" x="${padL + band * i}" y="${padT}" width="${band}" height="${ih}"/>
        <text class="axis" x="${cx}" y="${padT + ih + 22}" text-anchor="middle">${label}</text>
      </g>`;
    }).join("");

    $("#chartBox").innerHTML = `<svg viewBox="0 0 ${W} ${H}" role="group" aria-label="Books by genre, column chart">${grid}${cols}</svg>`;

    const tip = $("#chartTip"), card = $(".chart-card"), svg = $("#chartBox svg");
    const showTip = g => {
      const d = data[+g.dataset.i];
      $$(".col", svg).forEach(c => c.classList.toggle("on", c === g));
      tip.innerHTML = `<strong>${esc(d.genre)}</strong>${d.titles} title${d.titles === 1 ? "" : "s"} · ${d.copies} copies`;
      tip.hidden = false;
      const bar = g.querySelector(".bar").getBoundingClientRect(), box = card.getBoundingClientRect();
      const left = Math.max(70, Math.min(box.width - 70, bar.left - box.left + bar.width / 2));
      tip.style.left = left + "px";
      tip.style.top = (bar.top - box.top - 8) + "px";
    };
    const hideTip = () => { tip.hidden = true; $$(".col", svg).forEach(c => c.classList.remove("on")); };
    $$(".col", svg).forEach(g => {
      g.addEventListener("pointerenter", () => showTip(g));
      g.addEventListener("focus", () => showTip(g));
      g.addEventListener("blur", hideTip);
    });
    svg.addEventListener("pointerleave", hideTip);
  };

  window.addEventListener("resize", () => { if (!$("[data-page=chart]").hidden) $("#chartTip").hidden = true; });

  // ---------- Splash -> first screen ----------
  // "?demo" in the link opens the app straight in the demo account
  const demoLink = new URLSearchParams(location.search).has("demo");
  if (demoLink && !currentUser) { currentUser = "demo"; store.set(SESSION_KEY, "demo", sessionStorage); }
  const seenSplash = demoLink || store.get("ravan-splash", sessionStorage);
  function afterSplash() {
    $("#splash").classList.add("out");
    setTimeout(() => ($("#splash").hidden = true), 400);
    if (currentUser) showApp(); else showAuth(db.users.length > 1 ? "login" : "register");
  }
  if (seenSplash) { $("#splash").hidden = true; afterSplash(); }
  else { store.set("ravan-splash", "1", sessionStorage); setTimeout(afterSplash, 1500); }

})();
