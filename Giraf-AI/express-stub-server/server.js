const express = require('express');
const app = express();
app.use(express.json());

app.post('/v1/images/generations', (req, res) => {
    console.log('Received body:', req.body);

    res.json({
        data: ["574893573489"]
    });
});

app.listen(8000, () => {
    console.log('Stub server running at http://localhost:8000');
});