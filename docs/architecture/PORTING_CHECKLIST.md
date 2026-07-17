# Module Porting Checklist

- Branch starts from current canonical `main`.
- Original PR and source commit are recorded.
- Files are placed according to `config/repository/module-boundaries.v1.json`.
- Duplicate implementations are removed or explicitly deprecated.
- Cross-language contracts are versioned.
- Legacy files remain inert.
- Required CI is green.
- Merge uses expected-head SHA enforcement.
- Superseded PR receives the replacement link before closure.
