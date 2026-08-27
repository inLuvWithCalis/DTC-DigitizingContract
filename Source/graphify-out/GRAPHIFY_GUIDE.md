# Hướng dẫn cài đặt và sử dụng Graphify

Tài liệu này áp dụng cho repository `DTC-DigitizingContract/Source`, ưu tiên Windows, PowerShell và Codex.

Graphify tạo một knowledge graph từ source code và tài liệu của dự án. Thay vì đọc hoặc tìm kiếm toàn bộ repository cho mỗi câu hỏi, Codex có thể query graph để lấy đúng các node, quan hệ và vị trí source liên quan.

> Gói chính thức trên PyPI tên là `graphifyy` (hai chữ `y`), nhưng lệnh CLI vẫn là `graphify`.

## 1. Các file quan trọng

| File/thư mục                                     | Vai trò                                                                  | Có nên commit?                                       |
| ------------------------------------------------ | ------------------------------------------------------------------------ | ---------------------------------------------------- |
| `graphify-out/graph.json`                        | Knowledge graph chính                                                    | Có                                                   |
| `graphify-out/GRAPH_REPORT.md`                   | Báo cáo kiến trúc, god nodes và communities                              | Có                                                   |
| `graphify-out/manifest.json`                     | Mốc để phát hiện file mới/thay đổi; dùng path tương đối nên chia sẻ được | Có                                                   |
| `AGENTS.md`                                      | Yêu cầu Codex ưu tiên query Graphify cho câu hỏi codebase                | Có                                                   |
| `.graphifyignore`                                | Quy định file nào không được đưa vào graph                               | Có                                                   |
| `.codex/skills/graphify/`                        | Skill cài cục bộ cho Codex                                               | Không trong cấu hình hiện tại; mỗi thành viên tự cài |
| `graphify-out/cache/`, `memory/`, `reflections/` | Cache và lịch sử làm việc riêng trên máy                                 | Không                                                |
| `graphify-out/.graphify_python`                  | Đường dẫn Python tuyệt đối của máy hiện tại                              | Không                                                |

Cấu hình `.gitignore` hiện tại của dự án chỉ chia sẻ ba file portable trong `graphify-out`: `graph.json`, `GRAPH_REPORT.md` và `manifest.json`.

## 2. Cài đặt lần đầu trên Windows

### 2.1. Cài `uv`

Mở PowerShell:

```powershell
winget install astral-sh.uv
```

Đóng và mở lại PowerShell, sau đó kiểm tra:

```powershell
uv --version
```

Graphify yêu cầu Python 3.10 trở lên. `uv` sẽ quản lý môi trường công cụ độc lập, tránh xung đột với Python của Backend hoặc các project khác.

### 2.2. Cài Graphify CLI

```powershell
uv tool install graphifyy
uv tool update-shell
```

Đóng và mở lại PowerShell, rồi kiểm tra:

```powershell
graphify --version
```

Nếu đã cài và muốn nâng cấp:

```powershell
uv tool upgrade graphifyy
```

Sau khi nâng cấp Graphify, nên cài lại skill và Git hook để chúng dùng đúng phiên bản/interpreter mới:

```powershell
graphify install --project --platform codex
graphify hook install
```

### 2.3. Cài skill Graphify cho Codex trong project

Đi tới thư mục gốc của repository:

```powershell
cd E:\DTC-DigitizingContract\Source
graphify install --project --platform codex
```

Kiểm tra file sau đã được tạo:

```text
.codex/skills/graphify/SKILL.md
```

Khởi động lại Codex sau khi cài để Codex nhận skill mới.

Repository này đang ignore `.codex/`, vì vậy mỗi thành viên cần chạy lệnh cài skill trên máy của mình sau khi clone. Không copy `.codex/` từ máy người khác vì nó có thể chứa cấu hình cục bộ.

### 2.4. Bật hướng dẫn always-on cho Codex

Repository đã có khối `graphify` trong `AGENTS.md`. Nếu thiết lập một repository khác chưa có khối này, chạy:

```powershell
graphify codex install
```

Lệnh này có thể sửa `AGENTS.md` và cấu hình hook của Codex. Luôn review diff trước khi commit để không ghi đè các quy tắc dự án đang có.

Nếu full semantic build báo Codex không có `spawn_agent`, thêm vào file cấu hình Codex của người dùng rồi khởi động lại Codex:

```toml
[features]
multi_agent = true
```

### 2.5. Cài Git hook tự động

> Cảnh báo riêng cho repository này: Git root là `E:\DTC-DigitizingContract`, nhưng root được index là `E:\DTC-DigitizingContract\Source`. Graphify 0.9.50 chạy Git hook tại Git root, nên hook mặc định có thể sinh nhầm `E:\DTC-DigitizingContract\graphify-out`. Không cài hook mặc định nếu vẫn muốn giữ graph tại `Source\graphify-out`.

Với repository này, workflow được khuyến nghị là update thủ công từ thư mục `Source`:

```powershell
cd E:\DTC-DigitizingContract\Source
graphify update .
```

Các lệnh dưới đây chỉ phù hợp khi thư mục được index cũng chính là Git root, hoặc đội đã có custom hook trỏ rõ về `Source`:

```powershell
graphify hook install
graphify hook status
```

Hook cập nhật AST code sau `git commit`/`git checkout` mà không cần API key, đồng thời cấu hình merge driver cho `graph.json`.

Lưu ý:

- Hook chỉ xử lý code; thay đổi tài liệu, PDF và ảnh vẫn cần semantic update thủ công.
- Hook chạy sau commit nên có thể làm các file trong `graphify-out/` trở thành dirty. Đây là trạng thái bình thường.
- Sau khi cài lại hoặc nâng cấp Graphify, chạy lại `graphify hook install` để refresh đường dẫn interpreter được ghi trong hook.

Nếu đã lỡ cài hook mặc định trong repository này, tắt nó bằng:

```powershell
graphify hook uninstall
```

Sau đó có thể xóa `E:\DTC-DigitizingContract\graphify-out`; không xóa `E:\DTC-DigitizingContract\Source\graphify-out`.

## 3. Sau khi mới clone repository này

Vì repository đã chia sẻ `graphify-out/graph.json`, không cần full rebuild ngay. Làm theo thứ tự:

```powershell
cd E:\DTC-DigitizingContract\Source
uv tool install graphifyy
graphify install --project --platform codex
graphify check-update .
```

Nếu `graphify check-update .` cho biết code đã thay đổi so với manifest, chạy:

```powershell
graphify update .
```

Sau đó có thể query graph ngay.

## 4. Tạo graph lần đầu khi project chưa có `graphify-out/graph.json`

### Cách A — chỉ index source code, chạy local và không cần API key

```powershell
graphify extract . --code-only
```

Cách này phù hợp để tạo graph nền nhanh và không gửi nội dung ra dịch vụ LLM. Nó bỏ qua tài liệu, PDF và ảnh.

### Cách B — tạo graph đầy đủ bằng Codex

Nhập trong ô chat của Codex, không nhập vào PowerShell:

```text
/graphify . --no-viz
```

`--no-viz` chỉ bỏ việc sinh HTML; nó không có nghĩa là code-only. Codex vẫn có thể semantic-extract tài liệu, PDF và ảnh bằng model của phiên làm việc.

Nếu muốn có giao diện graph HTML, bỏ `--no-viz`:

```text
/graphify .
```

### Cách C — semantic extraction bằng CLI và API key

Chỉ dùng khi đội đã chọn backend và chấp nhận gửi tài liệu được index tới backend đó. Ví dụ với Gemini trong PowerShell:

```powershell
$env:GEMINI_API_KEY = "your-key-for-current-shell"
graphify extract . --backend gemini
```

Không ghi API key vào repository, `.env` dùng chung hoặc tài liệu này.

Nếu gặp lỗi `no LLM API key found`, có hai lựa chọn:

- Chỉ cần code: chạy `graphify extract . --code-only`.
- Cần cả docs/PDF/ảnh: chạy `/graphify .` trong Codex hoặc cấu hình một semantic backend hợp lệ.

## 5. Khi nào cần update graph?

Không cần update sau mỗi prompt. Chỉ update khi nguồn dữ liệu của graph đã thay đổi.

Nên update khi:

- Thêm, xóa hoặc đổi tên class, interface, method, DTO, entity hay API endpoint.
- Thay đổi luồng gọi giữa Frontend và Backend.
- Refactor khiến quan hệ import/call/implement thay đổi.
- Vừa `git pull`, merge, rebase hoặc checkout sang nhánh có code khác đáng kể.
- Muốn Codex phân tích chính xác các thay đổi chưa commit hiện tại.
- Thêm hoặc sửa tài liệu nghiệp vụ, Markdown, DOCX, PDF hay ảnh mà muốn chúng xuất hiện trong graph.

Thường không cần update khi:

- Chỉ sửa typo hoặc format không làm thay đổi symbol/quan hệ code.
- Chỉ đổi CSS nhỏ và câu hỏi tiếp theo không liên quan đến thay đổi đó.
- `graphify check-update .` báo không có gì cần cập nhật.
- Chỉ muốn hỏi thêm về một graph đã cập nhật.

## 6. Các cách update graph

### 6.1. Kiểm tra trước khi update

```powershell
graphify check-update .
```

Đây là bước read-only, phù hợp để chạy sau `git pull` hoặc trước khi bắt đầu phân tích code.

### 6.2. Update code thông thường

```powershell
graphify update .
```

Lệnh CLI này re-extract code mới/thay đổi bằng AST local, cập nhật file bị xóa và recluster graph. Không cần API key.

Đây là lệnh mặc định nên dùng sau khi sửa `.cs`, `.ts`, `.tsx`, `.js` hoặc các source code được Graphify hỗ trợ.

### 6.3. Update nhanh khi đang code nhiều

```powershell
graphify update . --no-cluster
```

Lệnh này cập nhật graph code nhưng bỏ qua clustering. Query cơ bản vẫn dùng được, nhưng community và báo cáo kiến trúc chưa được làm mới đầy đủ.

Khi kết thúc một nhóm thay đổi, chạy clustering một lần:

```powershell
graphify cluster-only . --no-label --no-viz
```

Trong đó:

- `cluster-only`: không đọc hoặc extract source lại; chỉ phân cụm graph hiện có.
- `--no-label`: không gọi LLM để đặt tên community, giữ tên `Community N`.
- `--no-viz`: không tạo `graph.html`, hữu ích với graph lớn hơn khoảng 5.000 node.

Nếu muốn Graphify đặt tên community và đã cấu hình backend, bỏ `--no-label`.

### 6.4. Update tài liệu, PDF hoặc ảnh

`graphify update .` trong terminal chỉ dành cho code AST. Khi tài liệu/PDF/ảnh thay đổi, nhập trong Codex:

```text
/graphify . --update --no-viz
```

Lệnh skill này chỉ semantic-extract các file mới/thay đổi dựa trên manifest, thay vì build lại toàn bộ corpus.

### 6.5. Theo dõi thay đổi code liên tục

```powershell
graphify watch .
```

Dùng khi đang refactor dài và muốn graph tự cập nhật theo thay đổi file. Dừng bằng `Ctrl+C`.

Không cần chạy `watch` thường trực nếu Git hook đã đáp ứng workflow của đội.

### 6.6. Update sau khi xóa/refactor lớn

Graphify có shrink guard để tránh vô tình ghi đè một graph tốt bằng graph nhỏ bất thường. Nếu đã chủ động xóa nhiều code và update bị từ chối vì graph có ít node hơn, chạy:

```powershell
graphify update . --force
```

Chỉ dùng `--force` sau khi đã xác nhận việc giảm node là đúng. Không dùng nó như cách xử lý mặc định cho mọi lỗi update.

### 6.7. Khi nào chỉ cần `cluster-only`?

Dùng `cluster-only` khi source không đổi nhưng muốn:

- Tính lại community sau một lần `update --no-cluster`.
- Làm mới report/graph visualization.
- Đổi cách đặt tên community hoặc backend gán nhãn.

`cluster-only` không đưa code mới vào graph. Nếu source đã đổi, phải chạy `update` trước.

## 7. Cách sử dụng graph hằng ngày

### Hỏi câu hỏi tổng quát

```powershell
graphify query "SubmitForApprovalAsync gọi những method nào?"
```

`query` mặc định dùng BFS, phù hợp để lấy bối cảnh rộng và các node gần nhất.

### Trace một chuỗi quan hệ

```powershell
graphify query "contract approval flow" --dfs --budget 3000
```

DFS phù hợp hơn khi cần lần theo một luồng cụ thể. `--budget` giới hạn kích thước output.

### Giải thích một method hoặc concept

```powershell
graphify explain "SubmitForApprovalAsync"
```

Kết quả cho biết node, source file và các quan hệ trực tiếp của nó.

### Tìm đường đi giữa hai concept

```powershell
graphify path "ContractController" "ContractAudit"
```

Lệnh này hữu ích khi cần biết hai module kết nối với nhau qua những class/method nào.

### Xem phần bị ảnh hưởng

```powershell
graphify affected "ContractService"
```

Phù hợp trước refactor để tìm các node phụ thuộc ngược vào một concept.

### Dùng trong prompt Codex

Có thể yêu cầu trực tiếp:

```text
Dùng graphify, không dùng rg, giải thích SubmitForApprovalAsync và cho tôi file, line cùng các method nó gọi trực tiếp.
```

Khi `graphify-out/graph.json` tồn tại và `AGENTS.md` đã cấu hình, Codex phải query graph trước rồi mới đọc source hẹp để xác minh khi cần.

## 8. Workflow đề xuất cho dự án này

### Bắt đầu ngày làm việc

```powershell
git pull
graphify check-update .
graphify update .
```

Nếu `check-update` báo không có thay đổi thì bỏ qua `update`.

### Trong lúc phát triển

Sau một nhóm thay đổi có ảnh hưởng kiến trúc:

```powershell
graphify update . --no-cluster
```

Sau đó dùng `query`, `path`, `explain` để kiểm tra impact hoặc hỗ trợ Codex hiểu code mới.

### Trước khi kết thúc task/PR

```powershell
graphify update .
git status --short
```

Review và stage ba artifact chia sẻ nếu chúng thay đổi hợp lệ:

```text
graphify-out/graph.json
graphify-out/GRAPH_REPORT.md
graphify-out/manifest.json
```

Nếu chỉ tài liệu/PDF/ảnh thay đổi, chạy `/graphify . --update --no-viz` trong Codex trước khi review các artifact.

## 9. Xử lý lỗi thường gặp

### `graphify` không được nhận diện

```powershell
uv tool update-shell
```

Đóng/mở lại terminal. Nếu vẫn lỗi:

```powershell
uv tool dir --bin
uv tool list
```

### Cài nhầm package

Tên package là `graphifyy`, không phải `graphify`:

```powershell
uv tool install graphifyy
```

Nếu chạy không cần cài, cú pháp đúng là:

```powershell
uvx --from graphifyy graphify --version
```

### Skill và CLI lệch version

```powershell
uv tool upgrade graphifyy
graphify install --project --platform codex
graphify hook install
```

Sau đó khởi động lại Codex.

### `no LLM API key found`

Repository có docs/PDF/ảnh nên full CLI extraction yêu cầu semantic backend. `--no-viz` không loại bỏ yêu cầu này; nó chỉ bỏ file HTML.

Nếu chỉ cần source code:

```powershell
graphify extract . --code-only
```

### Graph bị cũ

```powershell
graphify check-update .
graphify update .
```

Nếu vừa xóa/refactor lớn và gặp shrink guard, xác nhận thay đổi rồi mới dùng `--force`.

### `graph.json` bị conflict khi merge

Kiểm tra Git hook/merge driver:

```powershell
graphify hook status
graphify hook install
```

Không sửa conflict JSON lớn bằng tay nếu merge driver của Graphify có thể union-merge graph.

### Query trả kết quả quá rộng hoặc không đúng concept

- Dùng đúng tên class/method nếu biết.
- Dùng `explain` cho một node cụ thể.
- Dùng `path` khi biết điểm đầu và điểm cuối.
- Thêm `--dfs` khi muốn trace chuỗi gọi.
- Giảm hoặc tăng `--budget` tùy lượng context cần thiết.
- Nếu source vừa đổi, update graph trước khi query lại.

## 10. Quyền riêng tư và an toàn dữ liệu

- AST của source code được xử lý local và không cần LLM/API key.
- Docs, PDF và ảnh cần semantic extraction; nội dung có thể được gửi tới backend/model đã cấu hình.
- Dùng `--code-only` nếu source hoặc tài liệu có yêu cầu data residency nghiêm ngặt.
- Khai báo secret, file cá nhân và output sinh tự động trong `.graphifyignore`.
- Không commit API key, `.graphify_python`, cache hay work memory cá nhân.

## 11. Checklist ngắn cho thành viên mới

- [ ] Cài `uv`.
- [ ] Chạy `uv tool install graphifyy`.
- [ ] Chạy `graphify install --project --platform codex`.
- [ ] Khởi động lại Codex.
- [ ] Không cài hook mặc định khi graph nằm trong `Source` nhưng Git root nằm ở thư mục cha.
- [ ] Xác nhận `graphify-out/graph.json` đã có từ Git.
- [ ] Chạy `graphify check-update .`.
- [ ] Dùng `graphify query`, `path` hoặc `explain` trước khi đọc rộng source code.
- [ ] Update graph sau thay đổi code có ý nghĩa.
- [ ] Dùng `/graphify . --update` khi docs/PDF/ảnh thay đổi.

## Tài liệu chính thức

- Repository và README: <https://github.com/Graphify-Labs/graphify>
