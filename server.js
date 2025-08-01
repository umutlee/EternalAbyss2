const express = require('express');
const fs = require('fs');
const path = require('path');
const app = express();
const port = process.env.PORT || 5173;

// 静态文件服务
app.use(express.static(__dirname));

// 处理根路径请求
app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, 'index.html'));
});

// 处理Markdown文件请求
app.get('/*.md', (req, res) => {
  const filePath = path.join(__dirname, req.path);
  
  // 检查文件是否存在
  fs.access(filePath, fs.constants.F_OK, (err) => {
    if (err) {
      res.status(404).send('文件不存在');
      return;
    }
    
    // 读取文件内容
    fs.readFile(filePath, 'utf8', (err, data) => {
      if (err) {
        res.status(500).send('读取文件失败');
        return;
      }
      
      // 设置内容类型为纯文本
      res.setHeader('Content-Type', 'text/plain; charset=utf-8');
      res.send(data);
    });
  });
});

// 启动服务器
app.listen(port, () => {
  console.log(`服务器运行在 http://localhost:${port}`);
});