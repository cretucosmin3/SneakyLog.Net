import http from 'k6/http';

const apiPath = "http://localhost:5000/testing"

export const options = {
    vus: 5,
    duration: '20s',
};

export default () => {
    http.get(apiPath);
};