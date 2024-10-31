import http from 'k6/http';

const apiPath = "http://localhost:5000/testing/person/bob"

export const options = {
    vus: 5,
    duration: '10s',
};

export default () => {
    http.get(apiPath);
};