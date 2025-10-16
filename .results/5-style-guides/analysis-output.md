# analysis-output Style Guide

- Files under `.results/` are generated artifacts from the instruction-generation workflow; regenerate them through the prompt chain instead of editing by hand.
- Maintain mirrored filenames when downstream prompts expect alternate aliases (e.g., both `1-determine-techstack.md` and `1-techstack.md`) so automated steps can resolve references.
- Treat these outputs as disposable; check them into version control only when instructions explicitly require long-lived context for AI assistants.
