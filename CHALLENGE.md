# 🏗️ The October Build Challenge

**Pick one project. Build it with AI. Understand every line. Finish it by October 31.**

> *"If AI writes the code now, what's even the point of building side projects?"*
>
> The point changed, it didn't disappear. AI writes syntax in seconds. What it doesn't give you is the feel for whether that code is any good: why the query is slow, why the auth is leaking, why the "working" solution falls apart with 50 users. You only get that by building things and breaking them.

> **Not a .NET developer?** The reference solutions are C#, but every BUILD-GUIDE prompt, trap and break-it experiment works in any language. Build it in your stack and use the README as a reference for the design, not the syntax.

---

## The one rule

**Let AI speed you up. Never let it think for you.**

If you can't explain a line, you don't ship it.

---

## 1. Pick your level

Be honest. Finishing a Starter project beats abandoning an Expert one.

| Level | You're here if... | Projects | Time |
|---|---|---|---|
| **01 · Starter** | collections, an ORM, or `async` still feel a bit like magic | [01 Expense Tracker CLI](01%20-%20Expense%20Tracker%20CLI/BUILD-GUIDE.md) · [02 To-Do REST API](02%20-%20To-Do%20REST%20API/BUILD-GUIDE.md) · [03 Weather Dashboard](03%20-%20Weather%20Dashboard/BUILD-GUIDE.md) | 3-5 h |
| **02 · Intermediate** | You build APIs at work but haven't designed for abuse, races, and failure | [04 URL Shortener](04%20-%20URL%20Shortener/BUILD-GUIDE.md) · [05 Blog API with Auth](05%20-%20Blog%20API%20with%20Auth/BUILD-GUIDE.md) · [06 Background Job Runner](06%20-%20Background%20Job%20Runner/BUILD-GUIDE.md) | 5-10 h |
| **03 · Advanced** | You're solid in your stack and want AI *inside* your app, not just in your editor | [07 AI Support Chatbot](07%20-%20AI%20Support%20Chatbot/BUILD-GUIDE.md) · [08 Docs Q&A with RAG](08%20-%20Docs%20Q%26A%20with%20RAG/BUILD-GUIDE.md) · [09 Smart Invoice Parser](09%20-%20Smart%20Invoice%20Parser/BUILD-GUIDE.md) | 4-10 h |
| **04 · Expert** | You've called an LLM from code and want to build things other devs and tools plug into | [10 Your Own MCP Server](10%20-%20Your%20Own%20MCP%20Server/BUILD-GUIDE.md) · [11 AI Code Review Agent](11%20-%20AI%20Code%20Review%20Agent/BUILD-GUIDE.md) · [12 AI App in Production](12%20-%20AI%20App%20in%20Production/BUILD-GUIDE.md) | 5-15 h |

Every project folder has two files:

- **`BUILD-GUIDE.md`**: build it yourself, step by step: prompts to use, *"Check yourself"* questions, where AI usually gets it wrong, and experiments to break it on purpose.
- **`README.md`**: the finished, fully-commented reference solution. **Don't open the code until you're stuck or done.**

---

## 2. The 4-week plan

| Week | Do this |
|---|---|
| **Week 1** | Pick your project. Create an **empty repo**. Read the build guide once, top to bottom. Do Steps 1-2. |
| **Week 2** | Build the core steps. For every step: plan with AI → review → answer the "Check yourself" question **without AI**. |
| **Week 3** | Do the **"Break it on purpose"** experiments. Fix what breaks. This is where most of the learning happens. |
| **Week 4** | Tick every box in the **Definition of done**. Write your short write-up (below). Ship it. |

About 2-4 focused hours a week is enough for Starter/Intermediate. Advanced/Expert: plan for more.

---

## 3. How to work with AI during the challenge

Use this for every step:

1. **Outcome + constraints, not steps.** *"Add X. Keep Y unchanged. No Z."*
2. **Plan before edits.** *"Show me the plan first."* Approve it, then let it write.
3. **Review before accepting.** Read every line. Ask *"what inputs make this throw?"*
4. **Log the misses.** Keep an `AI-MISSES.md` in your repo. Every time the AI is wrong, write down what it did and how you caught it.

That last file is the real proof you learned something.

---

## 4. What "finished" means

Your project is done when:

- [ ] Every box in the project's **Definition of done** is ticked
- [ ] It's in a public GitHub repo with a short README (what, how to run, what you learned)
- [ ] Your `AI-MISSES.md` has at least **3 entries**
- [ ] You can explain any line if someone asks

---

## 5. Share it

Post your repo with:

```
Project: #__ <name>
Level: Starter / Intermediate / Advanced / Expert
Repo: <link>
The sneakiest thing AI got wrong: <one sentence>
What I'd do differently: <one sentence>
```

Share it on LinkedIn/X and tag **Stefan Đokić**, or post it inside the community (below). I read them.

---

## Want to do Advanced & Expert with support?

The Starter and Intermediate levels are very doable on your own with these guides.

**Advanced and Expert** are where most people get stuck: Ollama won't connect, the agent doesn't call tools, RAG answers with nonsense, the MCP client silently disconnects. That's what we do inside **[AI for .NET Developers](https://www.skool.com/ai-for-dotnet-developers/about)**:

- Source code, roadmaps, and video walkthroughs for the AI projects
- A place to post your challenge progress and get feedback
- Someone to ask when you get stuck

👉 **[Join AI for .NET Developers](https://www.skool.com/ai-for-dotnet-developers/about)**

---

*Built by [Stefan Đokić](https://thecodeman.net) · [Newsletter](https://thecodeman.net) · [YouTube](https://www.youtube.com/@thecodeman_) · [LinkedIn](https://www.linkedin.com/in/djokic-stefan/)*
